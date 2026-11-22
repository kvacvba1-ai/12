// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Systems.Combat;

/// <summary>
/// TRENCHFACE control: restrained face, heavy outer mounting hardware.
/// The label and hit target remain the stock Button implementation.
/// </summary>
public sealed class CombatPanelButton : Button
{
    public CombatPanelButton()
    {
        Label.FontColorOverride = new Color(218, 198, 155);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var size = PixelSizeBox.Size;
        if (size.X <= 8 || size.Y <= 8)
            return;

        var active = HasStyleClass("CombatTabActive");
        var primary = HasStyleClass("CombatPrimary");
        var disabled = Disabled;
        var pressed = HasStylePseudoClass("pressed");
        var hovered = HasStylePseudoClass("hover");

        var shadow = new Color(8, 11, 11);
        var frame = disabled ? new Color(56, 59, 54) : active || primary ? new Color(145, 103, 63) : new Color(83, 91, 84);
        var face = disabled ? new Color(29, 33, 32) : pressed ? new Color(28, 34, 33) : hovered ? new Color(53, 59, 51) : new Color(39, 46, 43);

        // One quiet face, with the thickness carried by the outside mount.
        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), shadow);
        handle.DrawRect(new UIBox2(3, 2, size.X - 3, size.Y - 2), frame);
        handle.DrawRect(new UIBox2(6, 5, size.X - 6, size.Y - 5), face);
        handle.DrawRect(new UIBox2(7, 5, size.X - 7, 7), new Color(111, 119, 105));
        handle.DrawRect(new UIBox2(7, size.Y - 7, size.X - 7, size.Y - 5), new Color(16, 21, 20));

        // Side lugs and top/bottom mounting tabs are deliberately outside
        // the label area; no corner mosaics, inner stripes or gear medallions.
        var lugHeight = MathF.Min(10f, size.Y * .28f);
        var mid = size.Y * .5f;
        handle.DrawRect(new UIBox2(-4, mid - lugHeight, 5, mid + lugHeight, ), shadow);
        handle.DrawRect(new UIBox2(size.X - 5, mid - lugHeight, size.X + 4, mid + lugHeight), shadow);
        handle.DrawRect(new UIBox2(-3, mid - lugHeight + 2, 4, mid + lugHeight - 2), frame);
        handle.DrawRect(new UIBox2(size.X - 4, mid - lugHeight + 2, size.X + 3, mid + lugHeight - 2), frame);

        var bolt = new Vector2(MathF.Min(9f, size.X * .12f), mid);
        handle.DrawCircle(bolt, 3.2f, shadow, true);
        handle.DrawCircle(bolt, 2.1f, new Color(160, 139, 103), true);
        handle.DrawCircle(new Vector2(size.X - bolt.X, mid), 3.2f, shadow, true);
        handle.DrawCircle(new Vector2(size.X - bolt.X, mid), 2.1f, new Color(160, 139, 103), true);

        Label.FontColorOverride = disabled ? new Color(125, 119, 102) : new Color(224, 207, 166);
    }
}
