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

[Serializable, NetSerializable]
public enum CombatPanelTreatmentStage : byte
{
    Begin,
    Hit,
}

/// <summary>
/// Starts a server-timed medical challenge or reports a single interaction.
/// Treatment quality is deliberately not accepted from the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class CombatPanelTreatmentEvent : EntityEventArgs
{
    public NetEntity Target { get; }
    public NetEntity Item { get; }
    public TargetBodyPart Part { get; }
    public CombatPanelTreatmentType Treatment { get; }
    public CombatPanelTreatmentStage Stage { get; }

    public CombatPanelTreatmentEvent(
        NetEntity target,
        NetEntity item,
        TargetBodyPart part,
        CombatPanelTreatmentType treatment,
        CombatPanelTreatmentStage stage)
    {
        Target = target;
        Item = item;
        Part = part;
        Treatment = treatment;
        Stage = stage;
    }
}
