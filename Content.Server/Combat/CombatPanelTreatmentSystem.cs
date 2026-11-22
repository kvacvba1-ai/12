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

namespace Content.Server.Combat;

/// <summary>
/// Server-side gate for TRENCHFACE medical mini-games.
/// The client never applies healing or reagents; it only reports the challenge result.
/// </summary>
public sealed class CombatPanelTreatmentSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HealingSystem _healing = default!;
    [Dependency] private readonly InjectorSystem _injectors = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CombatPanelTreatmentEvent>(OnTreatmentRequested);
    }

    private void OnTreatmentRequested(CombatPanelTreatmentEvent message, EntitySessionEventArgs args)
    {
        if (message.Quality == CombatPanelTreatmentQuality.Miss
            || args.SenderSession.AttachedEntity is not { } user
            || !EntityManager.EntityExists(user)
            || !TryGetEntity(message.Target, out var target)
            || target is not { } targetUid
            || !EntityManager.EntityExists(targetUid)
            || user == targetUid
            || Transform(user).MapUid != Transform(targetUid).MapUid
            || !TryGetEntity(message.Item, out var item)
            || item is not { } itemUid
            || !EntityManager.EntityExists(itemUid))
            return;

        // Only held or carried items are accepted. This includes both pockets,
        // backpack slot, hands and worn inventory slots; arbitrary world entities
        // cannot be injected or consumed through a forged network request.
        Entity<HandsComponent?, InventoryComponent?> inventoryOwner = (user, null, null);
        if (!_inventory.GetHandOrInventoryEntities(inventoryOwner).Contains(itemUid))
            return;

        switch (message.Treatment)
        {
            case CombatPanelTreatmentType.Bandage:
                if ((message.Part is TargetBodyPart.Head
                    or TargetBodyPart.Chest
                    or TargetBodyPart.Groin
                    or TargetBodyPart.LeftArm
                    or TargetBodyPart.RightArm
                    or TargetBodyPart.LeftLeg
                    or TargetBodyPart.RightLeg)
                    && HasComp<BodyComponent>(targetUid)
                    && HasComp<HealingComponent>(itemUid))
                {
                    _healing.TryStartCombatPanelHealing(user, targetUid, itemUid, message.Part);
                }
                break;

            case CombatPanelTreatmentType.Injection:
                if (HasComp<InjectorComponent>(itemUid))
                    _injectors.TryInjectFromCombatPanel(itemUid, user, targetUid);
                break;
        }
    }
}
