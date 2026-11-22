// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Targeting;
using Robust.Shared.Serialization;

namespace Content.Shared.Combat;

[Serializable, NetSerializable]
public enum CombatPanelTreatmentType : byte
{
    Bandage,
    Injection,
}

[Serializable, NetSerializable]
public enum CombatPanelTreatmentQuality : byte
{
    Miss,
    Good,
    Perfect,
}

/// <summary>
/// Requests a treatment after the client-side interaction challenge.
/// The server validates ownership and delegates the actual treatment to the
/// native HealingSystem or InjectorSystem.
/// </summary>
[Serializable, NetSerializable]
public sealed class CombatPanelTreatmentEvent : EntityEventArgs
{
    public NetEntity Target { get; }
    public NetEntity Item { get; }
    public TargetBodyPart Part { get; }
    public CombatPanelTreatmentType Treatment { get; }
    public CombatPanelTreatmentQuality Quality { get; }

    public CombatPanelTreatmentEvent(
        NetEntity target,
        NetEntity item,
        TargetBodyPart part,
        CombatPanelTreatmentType treatment,
        CombatPanelTreatmentQuality quality)
    {
        Target = target;
        Item = item;
        Part = part;
        Treatment = treatment;
        Quality = quality;
    }
}
