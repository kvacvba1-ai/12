// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

[Serializable, NetSerializable]
public sealed partial class HealingDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// Explicit limb chosen by a treatment UI. Null preserves normal targeting behavior.
    /// </summary>
    public EntityUid? TargetedPart { get; }

    public HealingDoAfterEvent(EntityUid? targetedPart = null)
    {
        TargetedPart = targetedPart;
    }
}