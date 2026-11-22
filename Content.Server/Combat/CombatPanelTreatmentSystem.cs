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
    private const float BandageTimeoutSeconds = 45f;
    private const float BandageMinimumWrapSeconds = 1.15f;
    private const float InjectionMarkerSpeed = 1.65f;
    private const float BandagePerfectHalfWidth = 0.07f;
    private const float BandageGoodHalfWidth = 0.20f;

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
            case CombatPanelTreatmentStage.Cancel:
                CancelChallenge(user, targetUid, itemUid, message);
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
            && now - active.StartedAt < TimeSpan.FromSeconds(active.Treatment == CombatPanelTreatmentType.Bandage
                ? BandageTimeoutSeconds
                : ChallengeTimeoutSeconds))
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

    private void CancelChallenge(
        EntityUid user,
        EntityUid target,
        EntityUid item,
        CombatPanelTreatmentEvent message)
    {
        if (_challenges.TryGetValue(user, out var challenge)
            && challenge.Target == target
            && challenge.Item == item
            && challenge.Part == message.Part
            && challenge.Treatment == message.Treatment)
        {
            _challenges.Remove(user);
        }
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
        var timeout = message.Treatment == CombatPanelTreatmentType.Bandage
            ? BandageTimeoutSeconds
            : ChallengeTimeoutSeconds;
        if (elapsed < 0.05 || elapsed > timeout)
        {
            _challenges.Remove(user);
            return;
        }

        if (!CanPerformTreatment(target, item, message.Treatment))
        {
            _challenges.Remove(user);
            return;
        }

        // A bandage is completed by winding the entire circumference with the
        // left mouse button. The client sends Hit only after the clockwise wrap
        // reaches 100%; the server enforces a minimum duration and never accepts
        // client-supplied treatment quality.
        if (message.Treatment == CombatPanelTreatmentType.Bandage)
        {
            _challenges.Remove(user);
            if (elapsed < BandageMinimumWrapSeconds)
                return;

            ApplyTreatment(user, target, item, message.Part, message.Treatment, CombatPanelTreatmentQuality.Perfect);
            return;
        }

        // Injection keeps its separate server-timed timing-bar challenge.
        var phase = (float) elapsed * InjectionMarkerSpeed % 2f;
        var markerPosition = 1f - MathF.Abs(phase - 1f);
        var distance = MathF.Abs(markerPosition - 0.5f);
        var quality = message.Treatment == CombatPanelTreatmentType.Bandage
            ? distance <= BandagePerfectHalfWidth
                ? CombatPanelTreatmentQuality.Perfect
                : distance <= BandageGoodHalfWidth
                    ? CombatPanelTreatmentQuality.Good
                    : CombatPanelTreatmentQuality.Miss
            : distance <= 0.04f
                ? CombatPanelTreatmentQuality.Perfect
                : distance <= 0.12f
                    ? CombatPanelTreatmentQuality.Good
                    : CombatPanelTreatmentQuality.Miss;

        _challenges.Remove(user);
        ApplyTreatment(user, target, item, message.Part, message.Treatment, quality);
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

    }
}
