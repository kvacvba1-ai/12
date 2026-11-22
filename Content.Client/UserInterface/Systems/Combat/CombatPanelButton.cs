// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Systems.Combat;

/// <summary>
/// TRENCHFACE forged-metal control. The stock button fill is covered by a
/// deterministic recessed steel plate; its label remains a normal child control.
/// </summary>
public sealed class CombatPanelButton : Button
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSizeBox.Size;
        if (size.X <= 2 || size.Y <= 2)
            return;

        var bounds = UIBox2.FromDimensions(Vector2.Zero, size);
        var disabled = Disabled;
        var pressed = HasStylePseudoClass("pressed");
        var hovered = HasStylePseudoClass("hover");

        var baseColor = disabled ? new Color(35, 38, 37)
            : pressed ? new Color(54, 45, 37)
            : hovered ? new Color(66, 67, 57)
            : new Color(43, 49, 47);
        var edge = disabled ? new Color(66, 68, 62) : new Color(135, 117, 87);
        var dark = new Color(16, 20, 20);
        var rust = new Color(111, 54, 34);
        var bone = disabled ? new Color(133, 128, 111) : new Color(220, 204, 164);

        handle.DrawRect(bounds, dark);
        handle.DrawRect(new UIBox2(2, 2, size.X - 2, size.Y - 2), edge);
        handle.DrawRect(new UIBox2(4, 4, size.X - 4, size.Y - 4), baseColor);

        // Bevelled top lip and deep lower seam.
        handle.DrawRect(new UIBox2(5, 5, size.X - 5, 7), new Color(184, 174, 143));
        handle.DrawRect(new UIBox2(5, size.Y - 7, size.X - 5, size.Y - 4), dark);
        handle.DrawRect(new UIBox2(7, size.Y - 4, size.X - 7, size.Y - 3), rust);

        // Short diagonal machining cuts and oxidized pits, kept away from the label centre.
        var marks = new (float X, float Y, float L)[]
        {
            (0.07f, 0.24f, 0.035f), (0.13f, 0.76f, 0.045f),
            (0.82f, 0.22f, 0.04f), (0.91f, 0.70f, 0.035f),
            (0.68f, 0.12f, 0.025f), (0.34f, 0.88f, 0.03f),
        };
        foreach (var mark in marks)
        {
            var x = size.X * mark.X;
            var y = size.Y * mark.Y;
            handle.DrawLine(new Vector2(x, y), new Vector2(x + size.X * mark.L, y - 2), edge);
            handle.DrawCircle(new Vector2(x - 2, y + 2), 1.2f, rust, true);
        }

        // Four recessed fasteners with slotted heads.
        var inset = MathF.Min(9f, MathF.Min(size.X, size.Y) * 0.22f);
        foreach (var p in new[]
        {
            new Vector2(inset, inset), new Vector2(size.X - inset, inset),
            new Vector2(inset, size.Y - inset), new Vector2(size.X - inset, size.Y - inset)
        })
        {
            handle.DrawCircle(p, 3.1f, dark, true);
            handle.DrawCircle(p, 2.1f, rust, true);
            handle.DrawCircle(p, 1.25f, edge, true);
            handle.DrawLine(p + new Vector2(-0.8f, 0), p + new Vector2(0.8f, 0), dark);
        }

        // A narrow red witness stripe identifies the primary/armed action.
        if (HasStyleClass("CombatPrimary"))
        {
            handle.DrawRect(new UIBox2(9, 9, 13, size.Y - 9), new Color(139, 39, 34));
            handle.DrawRect(new UIBox2(13, 9, 14, size.Y - 9), new Color(184, 105, 63));
        }

        // Thin warm metal glint on the upper edge, never a flat grey button.
        handle.DrawLine(new Vector2(15, 3), new Vector2(size.X - 15, 3), bone);
    }
}
