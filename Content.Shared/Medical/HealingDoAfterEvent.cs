// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Content.Shared._Shitmed.Targeting;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

[Serializable, NetSerializable]
public sealed partial class HealingDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// Explicit limb chosen by a treatment UI. Null preserves normal targeting behavior.
    /// </summary>
    public TargetBodyPart? TargetedPart { get; }

    public HealingDoAfterEvent(TargetBodyPart? targetedPart = null)
    {
        TargetedPart = targetedPart;
    }
}