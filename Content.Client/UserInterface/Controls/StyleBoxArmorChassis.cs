// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Separate outer chassis for TRENCHFACE. This is deliberately not a border:
/// it is a set of large mechanical assemblies surrounding the clean work area.
/// </summary>
public sealed class StyleBoxArmorChassis : StyleBox
{
    private static readonly Color Void = new(7, 10, 10);
    private static readonly Color Shadow = new(12, 16, 16);
    private static readonly Color Iron = new(48, 57, 55);
    private static readonly Color IronLight = new(133, 143, 130);
    private static readonly Color IronEdge = new(184, 184, 157);
    private static readonly Color Rust = new(133, 59, 34);
    private static readonly Color Signal = new(142, 39, 34);
    private static readonly Color Brass = new(166, 137, 91);

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var s = uiScale;
        var w = box.Width;
        var h = box.Height;
        if (w < 100f * s || h < 100f * s)
            return;

        // The chassis is intentionally discontinuous. Empty gaps expose the
        // dark/transparent space between the armor and the actual window.
        DrawTopBridge(handle, box, s);
        DrawLeftHydraulic(handle, box, s);
        DrawRightPivot(handle, box, s);
        DrawBottomStabilizer(handle, box, s);
        DrawCornerClamps(handle, box, s);
    }

    protected override float GetDefaultContentMargin(Margin margin) => 0f;

    private static void DrawTopBridge(DrawingHandleScreen handle, UIBox2 b, float s)
    {
        var cx = b.Left + b.Width * .5f;
        var y = b.Top + 3f * s;

        // A raised carrying yoke, with a recessed grip and two heavy feet.
        Rect(handle, b.Left + b.Width * .31f, y + 8f * s, b.Left + b.Width * .69f, y + 23f * s, Shadow);
        Rect(handle, b.Left + b.Width * .34f, y + 4f * s, b.Left + b.Width * .66f, y + 17f * s, Iron);
        Rect(handle, b.Left + b.Width * .38f, y + 7f * s, b.Left + b.Width * .62f, y + 11f * s, Void);
        Rect(handle, b.Left + b.Width * .35f, y + 3f * s, b.Left + b.Width * .65f, y + 5f * s, IronEdge);
        Rect(handle, b.Left + b.Width * .31f, y + 16f * s, b.Left + b.Width * .69f, y + 19f * s, Rust);

        // Grip ribs and the central locking boss make it read as hardware.
        for (var i = 0; i < 7; i++)
        {
            var x = cx - 25f * s + i * 8f * s;
            Rect(handle, x, y + 7f * s, x + 2f * s, y + 11f * s, IronLight);
        }

        Bolt(handle, new Vector2(b.Left + b.Width * .34f, y + 11f * s), 4.2f * s);
        Bolt(handle, new Vector2(b.Left + b.Width * .66f, y + 11f * s), 4.2f * s);
        Ring(handle, new Vector2(cx, y + 11f * s), 8f * s, 4f * s, Brass);
        Circle(handle, new Vector2(cx, y + 11f * s), 2.2f * s, Void);
    }

    private static void DrawLeftHydraulic(DrawingHandleScreen handle, UIBox2 b, float s)
    {
        var x = b.Left + 13f * s;
        var top = b.Top + b.Height * .23f;
        var bottom = b.Bottom - b.Height * .22f;
        var mid = (top + bottom) * .5f;

        // Main cylinder: multiple concentric sleeves, visible piston rod and
        // end caps. The assembly is much larger than a decorative tab.
        Rect(handle, x - 5f * s, top + 12f * s, x + 17f * s, bottom - 12f * s, Void);
        Rect(handle, x - 2f * s, top + 14f * s, x + 14f * s, bottom - 14f * s, Iron);
        Rect(handle, x + 2f * s, top + 18f * s, x + 10f * s, bottom - 18f * s, new Color(22, 29, 29));
        Rect(handle, x + 4f * s, top + 18f * s, x + 8f * s, bottom - 18f * s, IronLight);
        Rect(handle, x - 7f * s, top + 8f * s, x + 19f * s, top + 17f * s, IronLight);
        Rect(handle, x - 7f * s, bottom - 17f * s, x + 19f * s, bottom - 8f * s, IronLight);
        Rect(handle, x - 4f * s, top + 9f * s, x + 16f * s, top + 12f * s, IronEdge);
        Rect(handle, x - 4f * s, bottom - 12f * s, x + 16f * s, bottom - 9f * s, Rust);

        // A visible compression spring on the outside of the cylinder.
        for (var i = 0; i < 9; i++)
        {
            var yy = top + 25f * s + i * MathF.Max(5f * s, (bottom - top - 50f * s) / 9f);
            Rect(handle, x - 10f * s, yy, x - 3f * s, yy + 2.5f * s, Brass);
        }

        Ring(handle, new Vector2(x + 6f * s, top + 8f * s), 10f * s, 5f * s, IronEdge);
        Ring(handle, new Vector2(x + 6f * s, bottom - 8f * s), 10f * s, 5f * s, IronEdge);
        Bolt(handle, new Vector2(x + 6f * s, top + 8f * s), 3.5f * s);
        Bolt(handle, new Vector2(x + 6f * s, bottom - 8f * s), 3.5f * s);

        // Two diagonal braces tie the piston to the outer shell.
        ThickLine(handle, new Vector2(x + 14f * s, top + 18f * s), new Vector2(x + 27f * s, top + 5f * s), 4f * s, Iron);
        ThickLine(handle, new Vector2(x + 14f * s, bottom - 18f * s), new Vector2(x + 27f * s, bottom - 5f * s), 4f * s, Iron);
        Circle(handle, new Vector2(x + 25f * s, top + 5f * s), 4f * s, Rust);
        Circle(handle, new Vector2(x + 25f * s, bottom - 5f * s), 4f * s, Rust);

        // A small warning plate breaks the monochrome steel.
        Rect(handle, x - 8f * s, mid - 14f * s, x - 3f * s, mid + 14f * s, Signal);
        Rect(handle, x - 7f * s, mid - 10f * s, x - 4f * s, mid - 4f * s, Brass);
        Rect(handle, x - 7f * s, mid + 3f * s, x - 4f * s, mid + 9f * s, Brass);
    }

    private static void DrawRightPivot(DrawingHandleScreen handle, UIBox2 b, float s)
    {
        var center = new Vector2(b.Right - 18f * s, b.Top + b.Height * .30f);
        var radius = 22f * s;

        // Large articulated hinge with layered race, teeth and a deep axle.
        Rect(handle, center.X - 15f * s, center.Y - 27f * s, center.X + 3f * s, center.Y + 28f * s, Shadow);
        Rect(handle, center.X - 10f * s, center.Y - 24f * s, center.X + 7f * s, center.Y + 24f * s, Iron);
        Ring(handle, center, radius, 6f * s, IronEdge);
        Ring(handle, center, 16f * s, 5f * s, Rust);
        Ring(handle, center, 10f * s, 4f * s, Brass);
        Circle(handle, center, 5f * s, Void);
        Circle(handle, center, 2f * s, IronLight);

        for (var i = 0; i < 8; i++)
        {
            var a = MathF.Tau * i / 8f;
            var p = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 18f * s;
            Circle(handle, p, 2.3f * s, Void);
            Circle(handle, p, 1.2f * s, IronEdge);
        }

        // Short forked linkage leading into the pivot.
        Rect(handle, b.Right - 35f * s, center.Y - 5f * s, b.Right - 2f * s, center.Y + 5f * s, Shadow);
        Rect(handle, b.Right - 34f * s, center.Y - 3f * s, b.Right - 3f * s, center.Y + 2f * s, IronLight);
        Rect(handle, b.Right - 31f * s, center.Y + 8f * s, b.Right - 5f * s, center.Y + 13f * s, Rust);
        Bolt(handle, new Vector2(b.Right - 8f * s, center.Y + 10f * s), 3f * s);
    }

    private static void DrawBottomStabilizer(DrawingHandleScreen handle, UIBox2 b, float s)
    {
        var y = b.Bottom - 18f * s;
        var left = b.Left + b.Width * .29f;
        var right = b.Right - b.Width * .25f;

        // A heavy floating crossmember with a dark cavity and machined ribs.
        Rect(handle, left - 7f * s, y - 5f * s, right + 7f * s, y + 10f * s, Void);
        Rect(handle, left, y - 8f * s, right, y + 6f * s, Iron);
        Rect(handle, left + 5f * s, y - 8f * s, right - 5f * s, y - 4f * s, IronEdge);
        Rect(handle, left + 10f * s, y + 2f * s, right - 10f * s, y + 5f * s, Rust);

        for (var i = 0; i < 11; i++)
        {
            var x = left + 13f * s + i * MathF.Max(4f * s, (right - left - 26f * s) / 11f);
            Rect(handle, x, y - 2f * s, x + 2f * s, y + 2f * s, Void);
        }

        Bolt(handle, new Vector2(left - 1f * s, y - 1f * s), 4.5f * s);
        Bolt(handle, new Vector2(right + 1f * s, y - 1f * s), 4.5f * s);
        Rect(handle, (left + right) * .5f - 15f * s, y - 12f * s, (left + right) * .5f + 15f * s, y - 8f * s, Signal);
    }

    private static void DrawCornerClamps(DrawingHandleScreen handle, UIBox2 b, float s)
    {
        // Unequal corner armor gives the silhouette a deliberately assembled,
        // field-repaired character instead of a symmetrical picture frame.
        DrawClamp(handle, new Vector2(b.Left + 17f * s, b.Top + 20f * s), 13f * s, true);
        DrawClamp(handle, new Vector2(b.Right - 17f * s, b.Bottom - 20f * s), 13f * s, false);
        ThickLine(handle, new Vector2(b.Left + 5f * s, b.Bottom - 34f * s), new Vector2(b.Left + 26f * s, b.Bottom - 13f * s), 5f * s, Iron);
        ThickLine(handle, new Vector2(b.Right - 5f * s, b.Top + 42f * s), new Vector2(b.Right - 25f * s, b.Top + 22f * s), 5f * s, Iron);
    }

    private static void DrawClamp(DrawingHandleScreen handle, Vector2 center, float size, bool top)
    {
        var x = center.X;
        var y = center.Y;
        Rect(handle, x - size, y - size * .55f, x + size, y + size * .55f, Shadow);
        Rect(handle, x - size * .75f, y - size * .8f, x + size * .75f, y + size * .8f, Iron);
        Rect(handle, x - size * .65f, y - size * .8f, x + size * .65f, y - size * .55f, IronEdge);
        Rect(handle, x - size * .55f, y + size * .35f, x + size * .55f, y + size * .62f, Rust);
        Bolt(handle, center, size * .28f);
        if (top)
            Rect(handle, x - size * .9f, y - size * .95f, x + size * .9f, y - size * .75f, Brass);
    }

    private static void ThickLine(DrawingHandleScreen handle, Vector2 a, Vector2 b, float thickness, Color color)
    {
        var delta = b - a;
        var length = delta.Length();
        if (length < .1f)
            return;
        var normal = new Vector2(-delta.Y, delta.X) / length * thickness * .5f;
        handle.DrawLine(a + normal, b + normal, color);
        handle.DrawLine(a - normal, b - normal, color);
        handle.DrawLine(a, b, color);
    }

    private static void Bolt(DrawingHandleScreen handle, Vector2 center, float radius)
    {
        Circle(handle, center + new Vector2(1.2f, 1.6f), radius + 1.5f, Void);
        Circle(handle, center, radius, Rust);
        Circle(handle, center, radius * .68f, Brass);
        ThickLine(handle, center + new Vector2(-radius * .38f, 0), center + new Vector2(radius * .38f, 0), MathF.Max(1f, radius * .16f), Void);
    }

    private static void Ring(DrawingHandleScreen handle, Vector2 center, float outer, float inner, Color color)
    {
        Circle(handle, center, outer, color);
        Circle(handle, center, MathF.Max(0f, outer - inner), Shadow);
    }

    private static void Circle(DrawingHandleScreen handle, Vector2 center, float radius, Color color)
    {
        if (radius > 0f)
            handle.DrawCircle(center, radius, color, true);
    }

    private static void Rect(DrawingHandleScreen handle, float left, float top, float right, float bottom, Color color)
    {
        if (right > left && bottom > top)
            handle.DrawRect(new UIBox2(left, top, right, bottom), color);
    }
}
