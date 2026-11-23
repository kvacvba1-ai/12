// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Combat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Combat;

/// <summary>
/// Applies TRENCHFACE attacks from structured client requests.
/// The client supplies only the selected action/result; all damage and target
/// validation remain server-authoritative.
/// </summary>
public sealed class CombatPanelAttackSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly WoundSystem _wounds = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly GunSystem _guns = default!;

    private const float ShotDamage = 16f;
    private const float WeaponDamage = 16f;
    private const float PerfectWeaponBonus = 1.25f;
    private const float PunchDamage = 6f;
    private const float KickDamage = 8f;
    private const float AmputationIntegrityRatio = 0.20f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CombatPanelAttackEvent>(OnCombatPanelAttack);
    }

    private void OnCombatPanelAttack(CombatPanelAttackEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } attacker
            || !EntityManager.EntityExists(attacker)
            || !TryGetEntity(message.Target, out var target)
            || target is not { } targetUid
            || !EntityManager.EntityExists(targetUid)
            || attacker == targetUid
            || !HasComp<MobStateComponent>(targetUid)
            || Transform(attacker).MapUid != Transform(targetUid).MapUid)
            return;

        if (message.Part is not (TargetBodyPart.Head
            or TargetBodyPart.Chest
            or TargetBodyPart.LeftArm
            or TargetBodyPart.RightArm
            or TargetBodyPart.LeftLeg
            or TargetBodyPart.RightLeg))
            return;

        string damageType;
        float amount;

        switch (message.AttackType)
        {
            case CombatPanelAttackType.Shoot:
                damageType = "Piercing";
                amount = Math.Clamp(message.SuccessfulHits, 0, 2) * ShotDamage;
                break;
            case CombatPanelAttackType.Stab:
                damageType = "Slash";
                amount = message.Quality switch
                {
                    CombatPanelAttackQuality.Perfect => WeaponDamage * PerfectWeaponBonus,
                    CombatPanelAttackQuality.Good => WeaponDamage,
                    _ => 0f,
                };
                break;
            case CombatPanelAttackType.Punch:
                damageType = "Blunt";
                amount = PunchDamage;
                break;
            case CombatPanelAttackType.Kick:
                damageType = "Blunt";
                amount = KickDamage;
                break;
            default:
                return;
        }

        if (amount <= 0f)
            return;

        var partQuery = _body.ConvertTargetBodyPart(message.Part);
        var parts = _body.GetBodyChildrenOfTypeWithComponent<DamageableComponent>(
            targetUid,
            partQuery.Type,
            symmetry: partQuery.Symmetry).ToList();

        if (parts.Count == 0)
            return;

        // A role-play shot consumes one real round from the held gun, but never
        // invokes the firing pipeline and therefore never creates a projectile.
        // Resolve and apply the confirmed hit independently of the optional ammo-provider
        // bookkeeping. Ammo extraction is attempted, but a provider that cannot expose
        // its round through TakeAmmoEvent must never silently cancel an otherwise valid hit.
        if (message.AttackType == CombatPanelAttackType.Shoot)
            _guns.TryConsumeAmmoWithoutFiring(attacker);

        // Resolve the exact limb first; never route an explicit limb hit through the mob root.
        var (partUid, _, partDamageable) = parts[0];
        var damage = new DamageSpecifier(_prototypes.Index<DamageTypePrototype>(damageType), amount);
        var applied = _damageable.TryChangeDamage(
            partUid,
            damage,
            damageable: partDamageable,
            origin: attacker,
            canMiss: false);

        if (applied is null || applied.Empty)
            return;

        // Use the native wound/amputation path on the same entity that received damage.
        if (TryComp<WoundableComponent>(partUid, out var woundable)
            && woundable.CanRemove
            && woundable.ParentWoundable is { } parent
            && woundable.IntegrityCap > 0
            && (float) woundable.WoundableIntegrity <= (float) woundable.IntegrityCap * AmputationIntegrityRatio)
        {
            _wounds.AmputateWoundable(parent, partUid, woundable);
        }
    }
}
