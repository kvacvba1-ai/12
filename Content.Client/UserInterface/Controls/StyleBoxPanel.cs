// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Procedural TRENCHFACE industrial plate: layered gunmetal, oxidized edges,
/// worn paint, machining scratches, rivets and optional dried-blood marks.
/// </summary>
public sealed class StyleBoxPanel : StyleBox
{
    public Color BackgroundColor { get; set; } = new(25, 29, 30);
    public Color BorderColor { get; set; } = new(91, 101, 99);
    public Color InnerBorderColor { get; set; } = new(39, 47, 47);
    public Color HighlightColor { get; set; } = new(161, 169, 157);
    public Color AccentColor { get; set; } = new(139, 43, 39);
    public Color RivetColor { get; set; } = new(183, 157, 119);
    public Color RustColor { get; set; } = new(139, 67, 39);
    public Color ScratchColor { get; set; } = new(111, 119, 112);
    public Color BloodColor { get; set; } = new(105, 22, 27);
    public Thickness BorderThickness { get; set; } = new(3);
    public float CornerRadius { get; set; } = 12f;
    public float RivetRadius { get; set; } = 2.5f;
    public bool ShowRivets { get; set; } = true;
    public bool ShowAccent { get; set; } = true;
    public bool ShowWear { get; set; } = true;
    public bool ShowBlood { get; set; }
    public bool ShowHazardStripe { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var radius = MathF.Max(0f, CornerRadius * uiScale);
        var border = BorderThickness.Scale(uiScale);
        var left = border.Left;
        var top = border.Top;
        var right = border.Right;
        var bottom = border.Bottom;

        DrawRoundedRect(handle, box, radius, BorderColor);

        var inner = new UIBox2(
            box.Left + left,
            box.Top + top,
            MathF.Max(box.Left + left, box.Right - right),
            MathF.Max(box.Top + top, box.Bottom - bottom));
        var innerRadius = MathF.Max(0f, radius - MathF.Max(MathF.Max(left, right), MathF.Max(top, bottom)));
        DrawRoundedRect(handle, inner, innerRadius, InnerBorderColor);

        var content = new UIBox2(
            inner.Left + 1f * uiScale,
            inner.Top + 1f * uiScale,
            MathF.Max(inner.Left + 1f * uiScale, inner.Right - 1f * uiScale),
            MathF.Max(inner.Top + 1f * uiScale, inner.Bottom - 1f * uiScale));
        DrawRoundedRect(handle, content, MathF.Max(0f, innerRadius - uiScale), BackgroundColor);
        DrawSurfaceTexture(handle, content, uiScale);

        // Fine machined lip and a dark recessed seam make the plate read as layered steel.
        var lineY = inner.Top + MathF.Max(2f, innerRadius * 0.45f);
        handle.DrawLine(new Vector2(inner.Left + innerRadius, lineY),
            new Vector2(inner.Right - innerRadius, lineY), HighlightColor);
        handle.DrawLine(new Vector2(inner.Left + innerRadius, lineY + 2f * uiScale),
            new Vector2(inner.Right - innerRadius, lineY + 2f * uiScale), InnerBorderColor);

        if (ShowAccent)
        {
            var accentY = inner.Bottom - MathF.Max(2f, innerRadius * 0.45f);
            handle.DrawLine(new Vector2(inner.Left + innerRadius, accentY),
                new Vector2(inner.Right - innerRadius, accentY), AccentColor);
            handle.DrawLine(new Vector2(inner.Left + innerRadius, accentY + uiScale),
                new Vector2(inner.Right - innerRadius, accentY + uiScale), RustColor);
        }

        if (ShowHazardStripe)
            DrawHazardStripe(handle, box, uiScale);

        if (ShowWear)
            DrawWear(handle, box, uiScale);

        if (ShowBlood)
            DrawBlood(handle, box, uiScale);

        if (!ShowRivets)
            return;

        var rivet = MathF.Max(1f, RivetRadius * uiScale);
        var inset = MathF.Max(radius * 0.62f, rivet * 2.5f);
        var positions = new[]
        {
            new Vector2(box.Left + inset, box.Top + inset),
            new Vector2(box.Right - inset, box.Top + inset),
            new Vector2(box.Left + inset, box.Bottom - inset),
            new Vector2(box.Right - inset, box.Bottom - inset),
        };

        foreach (var position in positions)
        {
            handle.DrawCircle(position, rivet + uiScale, InnerBorderColor, true);
            handle.DrawCircle(position, rivet, RustColor, true);
            handle.DrawCircle(position, MathF.Max(1f, rivet * 0.28f), RivetColor, true);
            handle.DrawLine(position + new Vector2(-rivet * 0.55f, 0f),
                position + new Vector2(rivet * 0.55f, 0f), InnerBorderColor);
        }
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return margin switch
        {
            Margin.Top => BorderThickness.Top + CornerRadius * 0.35f,
            Margin.Bottom => BorderThickness.Bottom + CornerRadius * 0.35f,
            Margin.Left => BorderThickness.Left + CornerRadius * 0.35f,
            Margin.Right => BorderThickness.Right + CornerRadius * 0.35f,
            _ => 0f,
        };
    }

    private void DrawSurfaceTexture(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        // A fixed procedural brushed-metal grain. The pattern is stable between frames.
        var w = box.Width;
        var h = box.Height;
        var grainA = new Color(43, 51, 49);
        var grainB = new Color(17, 23, 23);

        // Broad, low-contrast tonal plates make the surface read as layered steel,
        // rather than a flat fill with decorative border lines.
        handle.DrawRect(new UIBox2(box.Left + w * 0.025f, box.Top + h * 0.18f,
            box.Left + w * 0.34f, box.Top + h * 0.43f), new Color(29, 36, 35));
        handle.DrawRect(new UIBox2(box.Left + w * 0.62f, box.Top + h * 0.56f,
            box.Right - w * 0.025f, box.Bottom - h * 0.12f), new Color(22, 29, 29));
        handle.DrawLine(new Vector2(box.Left + w * 0.035f, box.Top + h * 0.44f),
            new Vector2(box.Right - w * 0.035f, box.Top + h * 0.44f), new Color(12, 18, 18));
        handle.DrawLine(new Vector2(box.Left + w * 0.035f, box.Top + h * 0.445f),
            new Vector2(box.Right - w * 0.035f, box.Top + h * 0.445f), new Color(66, 72, 66));

        // Fine pitted grain and oil-darkened flecks. Integer modular sequences keep
        // every plate deterministic and avoid per-frame random allocations.
        for (var i = 0; i < 112; i++)
        {
            var xSeed = (i * 47 + 13) % 101;
            var ySeed = (i * 31 + 59) % 103;
            var x = box.Left + w * (0.025f + xSeed / 105f * 0.95f);
            var y = box.Top + h * (0.035f + ySeed / 108f * 0.93f);
            var radius = (0.35f + (i % 5) * 0.16f) * scale;
            var fleck = i % 11 == 0 ? new Color(106, 58, 37)
                : i % 4 == 0 ? new Color(12, 17, 17)
                : i % 3 == 0 ? new Color(65, 72, 68)
                : new Color(35, 43, 41);
            handle.DrawCircle(new Vector2(x, y), radius, fleck, true);
        }

        // Oily wipe marks and shallow brushed arcs catch the light at an angle.
        var wipes = new (float X, float Y, float L, float S)[]
        {
            (0.06f, 0.31f, 0.22f, -0.018f), (0.49f, 0.16f, 0.29f, 0.012f),
            (0.57f, 0.68f, 0.31f, -0.014f), (0.18f, 0.84f, 0.25f, 0.010f),
            (0.70f, 0.39f, 0.19f, 0.016f),
        };
        foreach (var wipe in wipes)
        {
            var start = new Vector2(box.Left + w * wipe.X, box.Top + h * wipe.Y);
            var end = start + new Vector2(w * wipe.L, h * wipe.S);
            handle.DrawLine(start + new Vector2(0f, 2f * scale),
                end + new Vector2(0f, 2f * scale), new Color(13, 18, 18));
            handle.DrawLine(start, end, new Color(55, 63, 60));
        }
        for (var row = 0; row < 9; row++)
        {
            var y = box.Top + h * (0.10f + row * 0.095f);
            handle.DrawLine(new Vector2(box.Left + w * 0.035f, y),
                new Vector2(box.Right - w * 0.035f, y + ((row % 3) - 1) * scale), grainA);
            handle.DrawLine(new Vector2(box.Left + w * 0.08f, y + 1.5f * scale),
                new Vector2(box.Right - w * 0.08f, y + 1.5f * scale), grainB);
        }

        // Uneven pits and tiny oxidation blooms across the plate, not only on its border.
        for (var i = 0; i < 72; i++)
        {
            var xSeed = (i * 37 + 17) % 97;
            var ySeed = (i * 53 + 29) % 89;
            var x = box.Left + w * (0.035f + xSeed / 100f * 0.93f);
            var y = box.Top + h * (0.045f + ySeed / 100f * 0.91f);
            var radius = (0.45f + (i % 4) * 0.22f) * scale;
            var color = i % 4 == 0 ? RustColor : i % 3 == 0 ? grainB : grainA;
            handle.DrawCircle(new Vector2(x, y), radius, color, true);
        }

        // Long, shallow abrasion marks with broken highlights and dark undersides.
        var scratches = new (float X, float Y, float L, float S)[]
        {
            (0.08f, 0.19f, 0.11f, 0.018f), (0.72f, 0.14f, 0.16f, -0.012f),
            (0.22f, 0.42f, 0.07f, 0.009f), (0.81f, 0.57f, 0.10f, -0.014f),
            (0.12f, 0.78f, 0.13f, -0.016f), (0.58f, 0.87f, 0.18f, 0.012f),
            (0.43f, 0.29f, 0.05f, -0.009f), (0.91f, 0.34f, 0.045f, 0.015f),
        };
        foreach (var scratch in scratches)
        {
            var start = new Vector2(box.Left + w * scratch.X, box.Top + h * scratch.Y);
            var end = start + new Vector2(w * scratch.L, h * scratch.S);
            handle.DrawLine(start + new Vector2(0, scale), end + new Vector2(0, scale), grainB);
            handle.DrawLine(start, end, ScratchColor);
        }

        // Dense hand-worked abrasion: paired gouges with dark cut edges and exposed steel.
        var deepScratches = new (float X, float Y, float L, float S)[] 
        {
            (0.16f, 0.16f, 0.17f, 0.012f), (0.162f, 0.164f, 0.13f, 0.010f),
            (0.67f, 0.23f, 0.21f, -0.018f), (0.672f, 0.236f, 0.16f, -0.015f),
            (0.28f, 0.61f, 0.12f, 0.025f), (0.282f, 0.616f, 0.09f, 0.021f),
            (0.74f, 0.78f, 0.15f, -0.022f), (0.742f, 0.786f, 0.11f, -0.018f),
            (0.48f, 0.91f, 0.08f, 0.008f), (0.482f, 0.914f, 0.06f, 0.006f),
        };

        foreach (var scratch in deepScratches)
        {
            var start = new Vector2(box.Left + w * scratch.X, box.Top + h * scratch.Y);
            var end = start + new Vector2(w * scratch.L, h * scratch.S);
            handle.DrawLine(start + new Vector2(0f, 1.4f * scale),
                end + new Vector2(0f, 1.4f * scale), new Color(11, 15, 15));
            handle.DrawLine(start, end, new Color(164, 157, 132));
        }

        // Heat-tinted weld beads and oxidized weld spatter along the plate seams.
        for (var i = 0; i < 26; i++)
        {
            var t = (i + 0.5f) / 26f;
            var x = box.Left + w * (0.08f + t * 0.84f);
            var y = i % 2 == 0 ? box.Top + h * 0.055f : box.Bottom - h * 0.055f;
            var bead = new Vector2(x, y);
            handle.DrawLine(bead, bead + new Vector2(3.5f * scale, 0f),
                i % 3 == 0 ? new Color(122, 72, 48) : new Color(91, 101, 96));
            if (i % 3 == 0)
                handle.DrawCircle(bead + new Vector2(1.5f, 1.5f) * scale,
                    1.15f * scale, new Color(145, 69, 39), true);
        }

        // Small impact dents: a dark crater, a cold-metal rim and a tiny bright catchlight.
        var dents = new (float X, float Y, float R)[]
        {
            (0.24f, 0.12f, 2.4f), (0.56f, 0.18f, 1.7f), (0.88f, 0.39f, 2.1f),
            (0.09f, 0.53f, 1.8f), (0.39f, 0.73f, 2.2f), (0.82f, 0.88f, 2.5f),
        };
        foreach (var dent in dents)
        {
            var point = new Vector2(box.Left + w * dent.X, box.Top + h * dent.Y);
            handle.DrawCircle(point, dent.R * scale, new Color(12, 16, 16), true);
            handle.DrawCircle(point + new Vector2(-0.45f, -0.45f) * scale,
                dent.R * 0.58f * scale, new Color(82, 89, 83), true);
            handle.DrawLine(point + new Vector2(-dent.R, -dent.R) * scale,
                point + new Vector2(-0.5f, -dent.R * 0.45f) * scale,
                new Color(178, 166, 135));
        }

        // Corrosion blooms collect near seams and fasteners.
        for (var i = 0; i < 18; i++)
        {
            var side = i % 4;
            var t = ((i * 29 + 11) % 83) / 100f;
            var p = side switch
            {
                0 => new Vector2(box.Left + w * t, box.Top + h * 0.035f),
                1 => new Vector2(box.Right - w * 0.035f, box.Top + h * t),
                2 => new Vector2(box.Left + w * t, box.Bottom - h * 0.035f),
                _ => new Vector2(box.Left + w * 0.035f, box.Top + h * t),
            };
            handle.DrawCircle(p, (1.1f + i % 3) * scale, RustColor, true);
            handle.DrawCircle(p + new Vector2(2f, 1f) * scale, 0.8f * scale, new Color(181, 103, 55), true);
        }
    }

    private void DrawWear(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        // Fixed, deliberately irregular marks: no per-frame randomness or flicker.
        var w = box.Width;
        var h = box.Height;
        var marks = new (float X, float Y, float Length, float Slope)[]
        {
            (0.12f, 0.035f, 0.045f, 0.012f), (0.31f, 0.025f, 0.030f, -0.008f),
            (0.73f, 0.030f, 0.052f, 0.010f), (0.91f, 0.11f, 0.020f, 0.035f),
            (0.055f, 0.68f, 0.018f, 0.055f), (0.94f, 0.72f, 0.020f, -0.045f),
            (0.19f, 0.965f, 0.060f, -0.012f), (0.62f, 0.975f, 0.035f, 0.008f),
            (0.84f, 0.955f, 0.045f, -0.010f),
        };

        foreach (var mark in marks)
        {
            var start = new Vector2(box.Left + w * mark.X, box.Top + h * mark.Y);
            var end = start + new Vector2(w * mark.Length, h * mark.Slope);
            handle.DrawLine(start, end, ScratchColor);
        }

        var pits = new (float X, float Y, float Radius)[]
        {
            (0.035f, 0.18f, 1.8f), (0.045f, 0.23f, 1.1f), (0.12f, 0.045f, 1.4f),
            (0.88f, 0.045f, 1.8f), (0.96f, 0.34f, 1.2f), (0.91f, 0.91f, 1.7f),
            (0.22f, 0.96f, 1.1f), (0.72f, 0.965f, 1.5f),
        };

        foreach (var pit in pits)
        {
            var point = new Vector2(box.Left + w * pit.X, box.Top + h * pit.Y);
            handle.DrawCircle(point, pit.Radius * scale, RustColor, true);
        }
    }

    private void DrawBlood(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        // Irregular dried splashes and dragged smears, concentrated around plate edges.
        var w = box.Width;
        var h = box.Height;
        var darkBlood = BloodColor;
        var driedEdge = new Color(66, 18, 20);
        var upper = new Vector2(box.Right - w * 0.16f, box.Top + h * 0.10f);

        handle.DrawCircle(upper, 6.5f * scale, driedEdge, true);
        handle.DrawCircle(upper + new Vector2(-4f, 1f) * scale, 4.2f * scale, darkBlood, true);
        handle.DrawCircle(upper + new Vector2(5f, 3f) * scale, 2.7f * scale, darkBlood, true);
        handle.DrawCircle(upper + new Vector2(-8f, 5f) * scale, 1.8f * scale, darkBlood, true);
        handle.DrawLine(upper + new Vector2(0f, 4f) * scale,
            upper + new Vector2(-3f, 19f) * scale, driedEdge);
        handle.DrawLine(upper + new Vector2(-2f, 6f) * scale,
            upper + new Vector2(-5f, 15f) * scale, darkBlood);
        handle.DrawLine(upper + new Vector2(4f, 5f) * scale,
            upper + new Vector2(8f, 13f) * scale, darkBlood);

        var lower = new Vector2(box.Left + w * 0.13f, box.Bottom - h * 0.11f);
        handle.DrawCircle(lower, 5.5f * scale, driedEdge, true);
        handle.DrawCircle(lower + new Vector2(3f, -1f) * scale, 3.4f * scale, darkBlood, true);
        handle.DrawCircle(lower + new Vector2(-5f, -3f) * scale, 2.1f * scale, darkBlood, true);
        handle.DrawLine(lower + new Vector2(2f, -1f) * scale,
            lower + new Vector2(11f, -8f) * scale, darkBlood);

        // Fine droplets around the two larger stains.
        var droplets = new (float X, float Y, float R)[]
        {
            (0.79f, 0.045f, 1.5f), (0.83f, 0.075f, 1.0f), (0.88f, 0.13f, 1.3f),
            (0.11f, 0.84f, 1.2f), (0.17f, 0.89f, 0.9f), (0.21f, 0.81f, 1.1f),
            (0.94f, 0.77f, 1.4f), (0.90f, 0.82f, 0.8f),
        };
        foreach (var drop in droplets)
        {
            var point = new Vector2(box.Left + w * drop.X, box.Top + h * drop.Y);
            handle.DrawCircle(point, drop.R * scale, darkBlood, true);
        }
    }

    private void DrawHazardStripe(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var stripeWidth = 5f * scale;
        var y = box.Bottom - 8f * scale;
        var start = box.Left + 20f * scale;
        var end = box.Right - 20f * scale;
        handle.DrawRect(new UIBox2(start, y, end, y + stripeWidth), AccentColor);
        for (var x = start; x < end; x += 16f * scale)
        {
            handle.DrawLine(new Vector2(x, y + stripeWidth),
                new Vector2(x + 7f * scale, y), RivetColor);
        }
    }

    private static void DrawRoundedRect(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        var width = MathF.Max(0f, box.Width);
        var height = MathF.Max(0f, box.Height);
        radius = Math.Clamp(radius, 0f, MathF.Min(width, height) * 0.5f);

        if (radius <= 0.5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        handle.DrawRect(new UIBox2(box.Left + radius, box.Top, box.Right - radius, box.Bottom), color);
        handle.DrawRect(new UIBox2(box.Left, box.Top + radius, box.Right, box.Bottom - radius), color);

        handle.DrawCircle(new Vector2(box.Left + radius, box.Top + radius), radius, color, true);
        handle.DrawCircle(new Vector2(box.Right - radius, box.Top + radius), radius, color, true);
        handle.DrawCircle(new Vector2(box.Left + radius, box.Bottom - radius), radius, color, true);
        handle.DrawCircle(new Vector2(box.Right - radius, box.Bottom - radius), radius, color, true);
    }
}
