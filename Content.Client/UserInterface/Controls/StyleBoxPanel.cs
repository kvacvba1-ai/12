// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Quiet structural plate for the actual UI surface. All expressive hardware
/// belongs to StyleBoxArmorChassis, outside this working area.
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
    public Thickness BorderThickness { get; set; } = new(3);
    public float CornerRadius { get; set; } = 8f;
    public float RivetRadius { get; set; } = 2.5f;
    public bool ShowRivets { get; set; }
    public bool ShowAccent { get; set; }
    public bool ShowWear { get; set; }
    public bool ShowBlood { get; set; }
    public bool ShowHazardStripe { get; set; }
    public bool ShowGear { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var border = BorderThickness.Scale(uiScale);
        var radius = MathF.Max(0f, CornerRadius * uiScale);
        DrawRoundedRect(handle, box, radius, BorderColor);

        var inner = new UIBox2(
            box.Left + border.Left,
            box.Top + border.Top,
            MathF.Max(box.Left + border.Left, box.Right - border.Right),
            MathF.Max(box.Top + border.Top, box.Bottom - border.Bottom));
        var innerRadius = MathF.Max(0f, radius - MathF.Max(
            MathF.Max(border.Left, border.Right),
            MathF.Max(border.Top, border.Bottom)));
        DrawRoundedRect(handle, inner, innerRadius, BackgroundColor);

        // A single broad machined highlight and a dark lower bevel. No
        // repeated frames, rivets, scratches, hazard bars or internal plates.
        var bevel = MathF.Max(1f * uiScale, MathF.Min(border.Top, border.Bottom) * .6f);
        handle.DrawRect(new UIBox2(
            box.Left + radius, box.Top + uiScale,
            box.Right - radius, box.Top + bevel), HighlightColor);
        handle.DrawRect(new UIBox2(
            box.Left + radius, box.Bottom - bevel,
            box.Right - radius, box.Bottom - uiScale), new Color(17, 21, 21));
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return margin switch
        {
            Margin.Top => BorderThickness.Top + CornerRadius * .2f,
            Margin.Bottom => BorderThickness.Bottom + CornerRadius * .2f,
            Margin.Left => BorderThickness.Left + CornerRadius * .2f,
            Margin.Right => BorderThickness.Right + CornerRadius * .2f,
            _ => 0f,
        };
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
