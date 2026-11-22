// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// TRENCHFACE armor frame. The face stays quiet and readable; visual weight
/// lives on the outer silhouette: cast shoulders, clamps and exposed hardware.
/// </summary>
public sealed class StyleBoxPanel : StyleBox
{
    public Color BackgroundColor { get; set; } = new(24, 29, 29);
    public Color BorderColor { get; set; } = new(91, 101, 99);
    public Color InnerBorderColor { get; set; } = new(39, 47, 47);
    public Color HighlightColor { get; set; } = new(161, 169, 157);
    public Color AccentColor { get; set; } = new(139, 43, 39);
    public Color RivetColor { get; set; } = new(183, 157, 119);
    public Color RustColor { get; set; } = new(139, 67, 39);
    public Color ScratchColor { get; set; } = new(111, 119, 112);
    public Color BloodColor { get; set; } = new(105, 22, 27);
    public Thickness BorderThickness { get; set; } = new(4);
    public float CornerRadius { get; set; } = 9f;
    public float RivetRadius { get; set; } = 2.5f;
    public bool ShowRivets { get; set; } = true;
    public bool ShowAccent { get; set; } = true;
    public bool ShowWear { get; set; }
    public bool ShowBlood { get; set; }
    public bool ShowHazardStripe { get; set; }
    public bool ShowGear { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var border = BorderThickness.Scale(uiScale);
        var left = border.Left;
        var top = border.Top;
        var right = border.Right;
        var bottom = border.Bottom;
        var radius = MathF.Max(0f, CornerRadius * uiScale);

        // Deep cast shadow and one substantial armor rim. No decorative
        // geometry is placed on the usable face of the panel.
        var shadow = Expand(box, 3f * uiScale);
        handle.DrawRect(shadow, new Color(8, 11, 12));
        DrawRoundedRect(handle, box, radius, BorderColor);

        var inner = new UIBox2(
            box.Left + left,
            box.Top + top,
            MathF.Max(box.Left + left, box.Right - right),
            MathF.Max(box.Top + top, box.Bottom - bottom));
        DrawRoundedRect(handle, inner, MathF.Max(0f, radius - MathF.Max(MathF.Max(left, right), MathF.Max(top, bottom))), BackgroundColor);

        // Broad bevels are concentrated on the perimeter, like welded armor.
        var bevel = MathF.Max(2f * uiScale, MathF.Max(MathF.Max(left, right), MathF.Max(top, bottom)) * .55f);
        handle.DrawRect(new UIBox2(box.Left + radius, box.Top + 1f * uiScale, box.Right - radius, box.Top + bevel), HighlightColor);
        handle.DrawRect(new UIBox2(box.Left + radius, box.Bottom - bevel, box.Right - radius, box.Bottom - 1f * uiScale), new Color(19, 23, 23));
        handle.DrawRect(new UIBox2(box.Left + 1f * uiScale, box.Top + radius, box.Left + bevel, box.Bottom - radius), new Color(123, 130, 119));
        handle.DrawRect(new UIBox2(box.Right - bevel, box.Top + radius, box.Right - 1f * uiScale, box.Bottom - radius), new Color(17, 21, 21));

        DrawExternalHardware(handle, box, uiScale);

        if (ShowRivets)
            DrawFasteners(handle, box, uiScale);
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return margin switch
        {
            Margin.Top => BorderThickness.Top + CornerRadius * 0.25f,
            Margin.Bottom => BorderThickness.Bottom + CornerRadius * 0.25f,
            Margin.Left => BorderThickness.Left + CornerRadius * 0.25f,
            Margin.Right => BorderThickness.Right + CornerRadius * 0.25f,
            _ => 0f,
        };
    }

    private void DrawExternalHardware(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var plate = MathF.Max(5f, 8f * scale);
        var reach = 7f * scale;

        // Four projecting armor ears. Their dark undersides create depth;
        // the lighter caps read as separate pieces bolted onto the shell.
        DrawClamp(handle, new UIBox2(box.Left + box.Width * .18f, box.Top, box.Left + box.Width * .34f, box.Top + plate), scale, true);
        DrawClamp(handle, new UIBox2(box.Right - box.Width * .34f, box.Top, box.Right - box.Width * .18f, box.Top + plate), scale, true);
        DrawClamp(handle, new UIBox2(box.Left + box.Width * .18f, box.Bottom - plate, box.Left + box.Width * .34f, box.Bottom), scale, false);
        DrawClamp(handle, new UIBox2(box.Right - box.Width * .34f, box.Bottom - plate, box.Right - box.Width * .18f, box.Bottom), scale, false);

        // Exposed side rails extend beyond the panel's nominal silhouette.
        handle.DrawRect(new UIBox2(box.Left - reach, box.Top + box.Height * .30f, box.Left + 2f * scale, box.Top + box.Height * .37f), new Color(10, 14, 14));
        handle.DrawRect(new UIBox2(box.Left - reach, box.Top + box.Height * .30f, box.Left + 1f * scale, box.Top + box.Height * .33f), RustColor);
        handle.DrawRect(new UIBox2(box.Right - 2f * scale, box.Top + box.Height * .63f, box.Right + reach, box.Top + box.Height * .70f), new Color(10, 14, 14));
        handle.DrawRect(new UIBox2(box.Right - 1f * scale, box.Top + box.Height * .63f, box.Right + reach, box.Top + box.Height * .66f), HighlightColor);

        if (ShowAccent || ShowHazardStripe)
        {
            // A single inset warning plate on the outside lower lip, not a stripe
            // running through the panel interior.
            var y = box.Bottom - 2f * scale;
            handle.DrawRect(new UIBox2(box.Left + box.Width * .42f, y, box.Left + box.Width * .58f, y + 5f * scale), new Color(22, 17, 16));
            handle.DrawRect(new UIBox2(box.Left + box.Width * .44f, y + scale, box.Left + box.Width * .56f, y + 3f * scale), AccentColor);
        }

        if (ShowWear || ShowBlood)
        {
            // Localized edge wear only; keep the center clean.
            var stain = ShowBlood ? BloodColor : RustColor;
            handle.DrawRect(new UIBox2(box.Left + 4f * scale, box.Top + box.Height * .78f, box.Left + 11f * scale, box.Top + box.Height * .84f), stain);
            handle.DrawRect(new UIBox2(box.Right - 10f * scale, box.Top + box.Height * .16f, box.Right - 3f * scale, box.Top + box.Height * .21f), RustColor);
        }
    }

    private static void DrawClamp(DrawingHandleScreen handle, UIBox2 box, float scale, bool top)
    {
        var offset = top ? -3f * scale : 3f * scale;
        var shadow = new UIBox2(box.Left - 2f * scale, box.Top + offset, box.Right + 2f * scale, box.Bottom + offset);
        handle.DrawRect(shadow, new Color(8, 11, 11));
        handle.DrawRect(box, new Color(54, 61, 57));
        handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + MathF.Max(2f, 2f * scale)), new Color(151, 154, 139));
        var cx = (box.Left + box.Right) * .5f;
        var cy = (box.Top + box.Bottom) * .5f;
        handle.DrawCircle(new System.Numerics.Vector2(cx, cy), MathF.Max(1.5f, 2f * scale), new Color(18, 22, 21), true);
    }

    private void DrawFasteners(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var radius = MathF.Max(1.5f, RivetRadius * scale);
        var inset = MathF.Max(CornerRadius * scale * .72f, radius * 2.5f);
        var positions = new[]
        {
            new System.Numerics.Vector2(box.Left + inset, box.Top + inset),
            new System.Numerics.Vector2(box.Right - inset, box.Top + inset),
            new System.Numerics.Vector2(box.Left + inset, box.Bottom - inset),
            new System.Numerics.Vector2(box.Right - inset, box.Bottom - inset),
        };

        foreach (var point in positions)
        {
            handle.DrawCircle(point + new System.Numerics.Vector2(1f, 1.5f) * scale, radius + 1.8f * scale, new Color(8, 11, 11), true);
            handle.DrawCircle(point, radius + .7f * scale, RustColor, true);
            handle.DrawCircle(point, radius * .62f, RivetColor, true);
            handle.DrawCircle(point, radius * .2f, new Color(37, 34, 29), true);
        }
    }

    private static UIBox2 Expand(UIBox2 box, float amount)
    {
        return new UIBox2(box.Left - amount, box.Top - amount, box.Right + amount, box.Bottom + amount);
    }

    private static void DrawRoundedRect(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        var width = MathF.Max(0f, box.Width);
        var height = MathF.Max(0f, box.Height);
        radius = Math.Clamp(radius, 0f, MathF.Min(width, height) * .5f);
        if (radius <= .5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        handle.DrawRect(new UIBox2(box.Left + radius, box.Top, box.Right - radius, box.Bottom), color);
        handle.DrawRect(new UIBox2(box.Left, box.Top + radius, box.Right, box.Bottom - radius), color);
        handle.DrawCircle(new System.Numerics.Vector2(box.Left + radius, box.Top + radius), radius, color, true);
        handle.DrawCircle(new System.Numerics.Vector2(box.Right - radius, box.Top + radius), radius, color, true);
        handle.DrawCircle(new System.Numerics.Vector2(box.Left + radius, box.Bottom - radius), radius, color, true);
        handle.DrawCircle(new System.Numerics.Vector2(box.Right - radius, box.Bottom - radius), radius, color, true);
    }
}
