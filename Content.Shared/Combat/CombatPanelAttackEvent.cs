// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Targeting;
using Robust.Shared.Serialization;

namespace Content.Shared.Combat;

[Serializable, NetSerializable]
public enum CombatPanelAttackType : byte
{
    Shoot,
    Stab,
    Punch,
    Kick,
}

[Serializable, NetSerializable]
public enum CombatPanelAttackQuality : byte
{
    Miss,
    Good,
    Perfect,
}

/// <summary>
/// Structured, server-authoritative TRENCHFACE attack request.
/// RP chat text is deliberately not used as gameplay input.
/// </summary>
[Serializable, NetSerializable]
public sealed class CombatPanelAttackEvent : EntityEventArgs
{
    public NetEntity Target { get; }
    public TargetBodyPart Part { get; }
    public CombatPanelAttackType AttackType { get; }
    public int SuccessfulHits { get; }
    public CombatPanelAttackQuality Quality { get; }

    public CombatPanelAttackEvent(
        NetEntity target,
        TargetBodyPart part,
        CombatPanelAttackType attackType,
        int successfulHits,
        CombatPanelAttackQuality quality)
    {
        Target = target;
        Part = part;
        AttackType = attackType;
        SuccessfulHits = successfulHits;
        Quality = quality;
    }
}
