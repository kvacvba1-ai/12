// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Systems.Combat;

/// <summary>
/// TRENCHFACE mechanical control: recessed glass face, armored side mounts,
/// engraved indicator rail and small status lamps.
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
        if (size.X <= 12 || size.Y <= 12)
            return;

        var active = HasStyleClass("CombatTabActive");
        var primary = HasStyleClass("CombatPrimary");
        var disabled = Disabled;
        var pressed = HasStylePseudoClass("pressed");
        var hovered = HasStylePseudoClass("hover");

        var shadow = new Color(7, 9, 9);
        var chassis = disabled
            ? new Color(48, 51, 48)
            : active || primary
                ? new Color(150, 111, 68)
                : new Color(91, 99, 89);
        var face = disabled
            ? new Color(25, 29, 28)
            : pressed
                ? new Color(25, 32, 31)
                : hovered
                    ? new Color(55, 63, 56)
                    : new Color(36, 43, 41);
        var inset = new Color(17, 21, 21);
        var brass = disabled ? new Color(79, 75, 62) : new Color(177, 151, 105);
        var lamp = disabled
            ? new Color(75, 67, 51)
            : active || primary
                ? new Color(224, 151, 61)
                : hovered
                    ? new Color(176, 139, 78)
                    : new Color(92, 103, 79);

        // Deep shadow and stepped armored bezel.
        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), shadow);
        handle.DrawRect(new UIBox2(2, 1, size.X - 2, size.Y - 1), chassis);
        handle.DrawRect(new UIBox2(5, 4, size.X - 5, size.Y - 4), inset);
        handle.DrawRect(new UIBox2(7, 6, size.X - 7, size.Y - 6), face);

        // Brushed-metal top lip and dark lower bevel.
        handle.DrawRect(new UIBox2(8, 6, size.X - 8, 8), new Color(133, 137, 119));
        handle.DrawRect(new UIBox2(8, size.Y - 8, size.X - 8, size.Y - 6), new Color(12, 16, 16));

        // Narrow signal rail: amber when selected, muted green at rest.
        handle.DrawRect(new UIBox2(10, 10, 13, size.Y - 10), inset);
        handle.DrawRect(new UIBox2(10, 11, 12, size.Y - 11), lamp);

        // Side mounting lugs and recessed fasteners.
        var lugHeight = MathF.Min(11f, size.Y * .25f);
        var mid = size.Y * .5f;
        handle.DrawRect(new UIBox2(-4, mid - lugHeight, 5, mid + lugHeight), shadow);
        handle.DrawRect(new UIBox2(size.X - 5, mid - lugHeight, size.X + 4, mid + lugHeight), shadow);
        handle.DrawRect(new UIBox2(-3, mid - lugHeight + 2, 4, mid + lugHeight - 2), chassis);
        handle.DrawRect(new UIBox2(size.X - 4, mid - lugHeight + 2, size.X + 3, mid + lugHeight - 2), chassis);

        var bolt = new Vector2(MathF.Min(9f, size.X * .12f), mid);
        handle.DrawCircle(bolt, 3.5f, shadow, true);
        handle.DrawCircle(bolt, 2.2f, brass, true);
        handle.DrawCircle(new Vector2(size.X - bolt.X, mid), 3.5f, shadow, true);
        handle.DrawCircle(new Vector2(size.X - bolt.X, mid), 2.2f, brass, true);

        // Twin pilot lamps at the far end make the control read as a physical switch.
        var lampX = size.X - 17f;
        handle.DrawCircle(new Vector2(lampX, 10f), 3.2f, shadow, true);
        handle.DrawCircle(new Vector2(lampX, 10f), 1.8f, lamp, true);
        handle.DrawCircle(new Vector2(lampX, size.Y - 10f), 3.2f, shadow, true);
        handle.DrawCircle(new Vector2(lampX, size.Y - 10f), 1.8f, brass, true);

        Label.FontColorOverride = disabled
            ? new Color(125, 119, 102)
            : active || primary
                ? new Color(244, 218, 166)
                : new Color(224, 207, 166);
    }
}
