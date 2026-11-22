// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Combat;
using Content.Shared.Inventory;
using Content.Shared.Medical.Healing;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Hands.Components;
using Content.Shared.Storage;
using Robust.Shared.Timing;

namespace Content.Server.Combat;

/// <summary>
/// Server-authoritative gate for TRENCHFACE medical mini-games.
/// The server owns challenge start time, hit windows, attempt counts and final quality.
/// </summary>
public sealed class CombatPanelTreatmentSystem : EntitySystem
{
    private const float ChallengeTimeoutSeconds = 8f;
    private const float RingRotationSpeed = MathF.PI;
    private const float RingSectorHalfWidth = 0.32f; // Includes a small allowance for network latency.
    private const float InjectionMarkerSpeed = 1.65f;
    private const int BandageAttempts = 2;

    private static readonly float[] RingTargetAngles =
    {
        MathF.PI * 7f / 6f,
        MathF.PI * 25f / 18f,
    };

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HealingSystem _healing = default!;
    [Dependency] private readonly InjectorSystem _injectors = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TreatmentChallenge> _challenges = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CombatPanelTreatmentEvent>(OnTreatmentRequested);
    }

    private void OnTreatmentRequested(CombatPanelTreatmentEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user
            || !EntityManager.EntityExists(user)
            || !TryGetEntity(message.Target, out var target)
            || target is not { } targetUid
            || !EntityManager.EntityExists(targetUid)
            || user == targetUid
            || Transform(user).MapUid != Transform(targetUid).MapUid
            || !TryGetEntity(message.Item, out var item)
            || item is not { } itemUid
            || !EntityManager.EntityExists(itemUid)
            || !Enum.IsDefined(message.Treatment)
            || !Enum.IsDefined(message.Stage)
            || !IsValidBodyPart(message.Part)
            || !IsCarriedBy(user, itemUid))
            return;

        switch (message.Stage)
        {
            case CombatPanelTreatmentStage.Begin:
                BeginChallenge(user, targetUid, itemUid, message);
                break;
            case CombatPanelTreatmentStage.Hit:
                RegisterHit(user, targetUid, itemUid, message);
                break;
        }
    }

    private void BeginChallenge(
        EntityUid user,
        EntityUid target,
        EntityUid item,
        CombatPanelTreatmentEvent message)
    {
        var now = _timing.CurTime;
        if (_challenges.TryGetValue(user, out var active)
            && now - active.StartedAt < TimeSpan.FromSeconds(ChallengeTimeoutSeconds))
            return;

        if (!CanPerformTreatment(target, item, message.Treatment))
            return;

        _challenges[user] = new TreatmentChallenge(
            target,
            item,
            message.Part,
            message.Treatment,
            now);
    }

    private void RegisterHit(
        EntityUid user,
        EntityUid target,
        EntityUid item,
        CombatPanelTreatmentEvent message)
    {
        if (!_challenges.TryGetValue(user, out var challenge)
            || challenge.Target != target
            || challenge.Item != item
            || challenge.Part != message.Part
            || challenge.Treatment != message.Treatment)
            return;

        var elapsed = (_timing.CurTime - challenge.StartedAt).TotalSeconds;
        if (elapsed < 0.05 || elapsed > ChallengeTimeoutSeconds)
        {
            _challenges.Remove(user);
            return;
        }

        if (!CanPerformTreatment(target, item, message.Treatment))
        {
            _challenges.Remove(user);
            return;
        }

        if (message.Treatment == CombatPanelTreatmentType.Bandage)
        {
            if (challenge.Attempts >= BandageAttempts)
                return;

            var angle = MathF.Min((float) elapsed * RingRotationSpeed, MathF.Tau);
            for (var i = 0; i < RingTargetAngles.Length; i++)
            {
                if (challenge.HitSectors.Contains(i))
                    continue;

                var delta = MathF.Abs(MathF.IEEERemainder(angle - RingTargetAngles[i], MathF.Tau));
                if (delta <= RingSectorHalfWidth)
                    challenge.HitSectors.Add(i);
            }

            challenge.Attempts++;
            if (challenge.Attempts < BandageAttempts)
                return;

            var quality = challenge.HitSectors.Count >= BandageAttempts
                ? CombatPanelTreatmentQuality.Perfect
                : challenge.HitSectors.Count == 1
                    ? CombatPanelTreatmentQuality.Good
                    : CombatPanelTreatmentQuality.Miss;

            _challenges.Remove(user);
            ApplyTreatment(user, target, item, message.Part, message.Treatment, quality);
            return;
        }

        // The injection marker moves from 0 to 1 and back at the same speed as the UI.
        // Its position is reconstructed from server time; no client-supplied quality is trusted.
        var phase = (float) elapsed * InjectionMarkerSpeed % 2f;
        var markerPosition = 1f - MathF.Abs(phase - 1f);
        var distance = MathF.Abs(markerPosition - 0.5f);
        var injectionQuality = distance <= 0.04f
            ? CombatPanelTreatmentQuality.Perfect
            : distance <= 0.12f
                ? CombatPanelTreatmentQuality.Good
                : CombatPanelTreatmentQuality.Miss;

        _challenges.Remove(user);
        ApplyTreatment(user, target, item, message.Part, message.Treatment, injectionQuality);
    }

    private bool CanPerformTreatment(EntityUid target, EntityUid item, CombatPanelTreatmentType treatment)
    {
        return treatment switch
        {
            CombatPanelTreatmentType.Bandage =>
                HasComp<BodyComponent>(target) && HasComp<HealingComponent>(item),
            CombatPanelTreatmentType.Injection =>
                HasComp<InjectorComponent>(item),
            _ => false,
        };
    }

    private void ApplyTreatment(
        EntityUid user,
        EntityUid target,
        EntityUid item,
        TargetBodyPart part,
        CombatPanelTreatmentType treatment,
        CombatPanelTreatmentQuality quality)
    {
        if (quality is not (CombatPanelTreatmentQuality.Good or CombatPanelTreatmentQuality.Perfect))
            return;

        switch (treatment)
        {
            case CombatPanelTreatmentType.Bandage:
                _healing.TryHealFromCombatPanel(user, target, item, part);
                break;
            case CombatPanelTreatmentType.Injection:
                _injectors.TryInjectFromCombatPanel(item, user, target);
                break;
        }
    }

    private bool IsCarriedBy(EntityUid user, EntityUid item)
    {
        // Only held or carried items are accepted, including nested storage containers.
        Entity<HandsComponent?, InventoryComponent?> inventoryOwner = (user, null, null);
        var pendingItems = new Queue<EntityUid>(_inventory.GetHandOrInventoryEntities(inventoryOwner));
        var visitedItems = new HashSet<EntityUid>();

        while (pendingItems.TryDequeue(out var carriedItem))
        {
            if (!visitedItems.Add(carriedItem) || !EntityManager.EntityExists(carriedItem))
                continue;

            if (carriedItem == item)
                return true;

            if (!TryComp<StorageComponent>(carriedItem, out var storage))
                continue;

            foreach (var contained in storage.Container.ContainedEntities)
                pendingItems.Enqueue(contained);
        }

        return false;
    }

    private static bool IsValidBodyPart(TargetBodyPart part)
    {
        return part is TargetBodyPart.Head
            or TargetBodyPart.Chest
            or TargetBodyPart.Groin
            or TargetBodyPart.LeftArm
            or TargetBodyPart.RightArm
            or TargetBodyPart.LeftLeg
            or TargetBodyPart.RightLeg;
    }

    private sealed class TreatmentChallenge(
        EntityUid target,
        EntityUid item,
        TargetBodyPart part,
        CombatPanelTreatmentType treatment,
        TimeSpan startedAt)
    {
        public EntityUid Target { get; } = target;
        public EntityUid Item { get; } = item;
        public TargetBodyPart Part { get; } = part;
        public CombatPanelTreatmentType Treatment { get; } = treatment;
        public TimeSpan StartedAt { get; } = startedAt;
        public int Attempts { get; set; }
        public HashSet<int> HitSectors { get; } = new();
    }
}
