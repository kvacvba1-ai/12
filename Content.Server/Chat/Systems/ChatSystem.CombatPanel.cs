// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Server-authoritative bridge between TRENCHFACE combat emotes and the existing
/// damage, wound, body-part and amputation systems.
/// </summary>
public sealed partial class ChatSystem
{
    [Dependency] private readonly DamageableSystem _combatDamageable = default!;
    [Dependency] private readonly SharedBodySystem _combatBody = default!;
    [Dependency] private readonly WoundSystem _combatWounds = default!;

    private const float CombatShotDamage = 16f;
    private const float CombatStabDamage = 16f;
    private const float CombatPunchDamage = 6f;
    private const float CombatKickDamage = 8f;
    private const float CombatAmputationIntegrityRatio = 0.20f;

    private void ApplyCombatPanelEmoteDamage(EntityUid source, string message)
    {
        if (!TryParseCombatPanelEmote(message, out var targetName, out var targetPart, out var damageType, out var damageAmount)
            || damageAmount <= 0f
            || !TryFindCombatPanelTarget(source, targetName, out var target))
            return;

        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(damageType), damageAmount);

        // Resolve the requested limb to its actual body-part entity first.
        // DamageableSystem.TryChangeDamage(target, targetPart: ...) can fall back
        // to Chest when the victim has no TargetingComponent, even if targetPart
        // was explicitly supplied. Applying damage to the part entity avoids that
        // routing bug and guarantees that the selected side is the one hit.
        var bodyPartQuery = _combatBody.ConvertTargetBodyPart(targetPart);
        var targetParts = _combatBody.GetBodyChildrenOfTypeWithComponent<DamageableComponent>(
            target,
            bodyPartQuery.Type,
            symmetry: bodyPartQuery.Symmetry).ToList();

        EntityUid? damagedPartUid = null;
        DamageSpecifier? applied;

        if (targetParts.Count > 0)
        {
            // A body-part type + symmetry should identify one limb. If a species has
            // multiple matching segments, keep the attack deterministic and hit the
            // first matching segment rather than spreading one shot across all of them.
            damagedPartUid = targetParts[0].Id;
            applied = _combatDamageable.TryChangeDamage(
                damagedPartUid.Value,
                damage,
                origin: source,
                canMiss: false);
        }
        else if (HasComp<TargetingComponent>(target))
        {
            // Some body setups keep DamageableComponent on the mob root instead of
            // each body-part entity. In that case use the engine's native targeting
            // route, but only when TargetingComponent is present: without it the
            // engine silently falls back to Chest for an explicitly selected limb.
            applied = _combatDamageable.TryChangeDamage(
                target,
                damage,
                origin: source,
                targetPart: targetPart,
                canMiss: false);
        }
        else
        {
            // Never silently turn a missing limb into chest damage.
            return;
        }

        if (applied is null || applied.Empty)
            return;

        // Check only the selected body-part type and symmetry. When damage was
        // applied directly, additionally require the exact same part entity.
        // The wound system updates integrity and the health analyzer's BodyStatus.
        var woundableParts = _combatBody.GetBodyChildrenOfTypeWithComponent<WoundableComponent>(
            target,
            bodyPartQuery.Type,
            symmetry: bodyPartQuery.Symmetry);

        foreach (var (partUid, _, woundable) in woundableParts)
        {
            if (damagedPartUid is { } exactPart && partUid != exactPart
                || !woundable.CanRemove
                || woundable.ParentWoundable is not { } parent
                || woundable.IntegrityCap <= 0
                || (float) woundable.WoundableIntegrity > (float) woundable.IntegrityCap * CombatAmputationIntegrityRatio)
                continue;

            _combatWounds.AmputateWoundable(parent, partUid, woundable);
            break;
        }
    }

    private bool TryFindCombatPanelTarget(EntityUid source, string targetName, out EntityUid target)
    {
        target = default;
        if (!EntityManager.EntityExists(source))
            return false;

        var sourceMap = Transform(source).MapUid;
        if (sourceMap == null)
            return false;

        var matches = 0;
        var query = EntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == source
                || !EntityManager.EntityExists(uid)
                || Transform(uid).MapUid != sourceMap
                || !string.Equals(Identity.Name(uid, EntityManager), targetName, StringComparison.OrdinalIgnoreCase))
                continue;

            target = uid;
            matches++;
            // Never guess if two characters on the same map share an identity.
            if (matches > 1)
                return false;
        }

        return matches == 1;
    }

    private static bool TryParseCombatPanelEmote(
        string message,
        out string targetName,
        out TargetBodyPart targetPart,
        out string damageType,
        out float damageAmount)
    {
        targetName = string.Empty;
        targetPart = TargetBodyPart.Chest;
        damageType = "Blunt";
        damageAmount = 0f;

        const string targetMarker = "пытается атаковать «";
        const string partMarker = "область «";

        var targetStart = message.IndexOf(targetMarker, StringComparison.OrdinalIgnoreCase);
        if (targetStart < 0)
            return false;

        targetStart += targetMarker.Length;
        var targetEnd = message.IndexOf("»:", targetStart, StringComparison.Ordinal);
        if (targetEnd <= targetStart)
            return false;

        targetName = message[targetStart..targetEnd].Trim();
        if (targetName.Length == 0)
            return false;

        var partStart = message.LastIndexOf(partMarker, StringComparison.OrdinalIgnoreCase);
        if (partStart < 0)
            return false;

        partStart += partMarker.Length;
        var partEnd = message.IndexOf('»', partStart);
        if (partEnd <= partStart)
            return false;

        var partName = message[partStart..partEnd].Trim().ToLowerInvariant();
        targetPart = partName switch
        {
            "голова" => TargetBodyPart.Head,
            "грудь" => TargetBodyPart.Chest,
            "левая рука" => TargetBodyPart.LeftArm,
            "правая рука" => TargetBodyPart.RightArm,
            "левая нога" => TargetBodyPart.LeftLeg,
            "правая нога" => TargetBodyPart.RightLeg,
            _ => (TargetBodyPart) 0,
        };

        if (targetPart == 0)
            return false;

        var action = message[..partStart].ToLowerInvariant();
        if (action.Contains("стреляет", StringComparison.Ordinal))
        {
            // The firearm timing ring reports the number of successful windows.
            const string hitMarker = "попаданий:";
            var hitStart = message.IndexOf(hitMarker, StringComparison.OrdinalIgnoreCase);
            if (hitStart < 0)
                return false;

            hitStart += hitMarker.Length;
            while (hitStart < message.Length && char.IsWhiteSpace(message[hitStart]))
                hitStart++;

            var hitEnd = message.IndexOf('/', hitStart);
            if (hitEnd <= hitStart || !int.TryParse(message[hitStart..hitEnd], out var hits))
                return false;

            damageType = "Piercing";
            damageAmount = Math.Clamp(hits, 0, 2) * CombatShotDamage;
            return true;
        }

        if (action.Contains("наносит удар оружием", StringComparison.Ordinal))
        {
            damageType = "Slash";
            damageAmount = message.Contains("идеально", StringComparison.OrdinalIgnoreCase)
                ? CombatStabDamage * 1.25f
                : message.Contains("удачно", StringComparison.OrdinalIgnoreCase)
                    ? CombatStabDamage
                    : 0f;
            return true;
        }

        if (action.Contains("бьёт кулаком", StringComparison.Ordinal))
        {
            damageType = "Blunt";
            damageAmount = CombatPunchDamage;
            return true;
        }

        if (action.Contains("наносит удар ногой", StringComparison.Ordinal))
        {
            damageType = "Blunt";
            damageAmount = CombatKickDamage;
            return true;
        }

        return false;
    }
}
