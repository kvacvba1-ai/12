// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A combat-panel frame with rounded corners, layered steel edging, inset shading and rivets.
/// Unlike StyleBoxFlat, this style box supports a configurable corner radius.
/// </summary>
public sealed class StyleBoxPanel : StyleBox
{
    public Color BackgroundColor { get; set; } = new(25, 23, 21);
    public Color BorderColor { get; set; } = new(126, 75, 49);
    public Color InnerBorderColor { get; set; } = new(65, 49, 40);
    public Color HighlightColor { get; set; } = new(174, 126, 83);
    public Color AccentColor { get; set; } = new(139, 43, 39);
    public Color RivetColor { get; set; } = new(183, 157, 119);
    public Thickness BorderThickness { get; set; } = new(3);
    public float CornerRadius { get; set; } = 12f;
    public float RivetRadius { get; set; } = 2.5f;
    public bool ShowRivets { get; set; } = true;
    public bool ShowAccent { get; set; } = true;

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

        var lineY = inner.Top + MathF.Max(2f, innerRadius * 0.45f);
        handle.DrawLine(
            new System.Numerics.Vector2(inner.Left + innerRadius, lineY),
            new System.Numerics.Vector2(inner.Right - innerRadius, lineY),
            HighlightColor);

        if (ShowAccent)
        {
            var accentY = inner.Bottom - MathF.Max(2f, innerRadius * 0.45f);
            handle.DrawLine(
                new System.Numerics.Vector2(inner.Left + innerRadius, accentY),
                new System.Numerics.Vector2(inner.Right - innerRadius, accentY),
                AccentColor);
        }

        if (!ShowRivets)
            return;

        var rivet = MathF.Max(1f, RivetRadius * uiScale);
        var inset = MathF.Max(radius * 0.62f, rivet * 2.5f);
        var positions = new[]
        {
            new System.Numerics.Vector2(box.Left + inset, box.Top + inset),
            new System.Numerics.Vector2(box.Right - inset, box.Top + inset),
            new System.Numerics.Vector2(box.Left + inset, box.Bottom - inset),
            new System.Numerics.Vector2(box.Right - inset, box.Bottom - inset),
        };

        foreach (var position in positions)
        {
            handle.DrawCircle(position, rivet + uiScale, BorderColor, true);
            handle.DrawCircle(position, rivet, RivetColor, true);
            handle.DrawCircle(position, MathF.Max(1f, rivet * 0.28f), InnerBorderColor, true);
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

        handle.DrawCircle(new System.Numerics.Vector2(box.Left + radius, box.Top + radius), radius, color, true);
        handle.DrawCircle(new System.Numerics.Vector2(box.Right - radius, box.Top + radius), radius, color, true);
        handle.DrawCircle(new System.Numerics.Vector2(box.Left + radius, box.Bottom - radius), radius, color, true);
        handle.DrawCircle(new System.Numerics.Vector2(box.Right - radius, box.Bottom - radius), radius, color, true);
    }
}
