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

        var lipHeight = MathF.Max(3f,4f*uiScale);
        handle.DrawRect(new UIBox2(inner.Left+innerRadius,inner.Top+2f*uiScale,inner.Right-innerRadius,inner.Top+2f*uiScale+lipHeight),new Color(115,124,115));
        handle.DrawRect(new UIBox2(inner.Left+innerRadius,inner.Top+2f*uiScale+lipHeight,inner.Right-innerRadius,inner.Top+2f*uiScale+lipHeight+3f*uiScale),new Color(25,31,30));
        if(ShowAccent)
        {
            var y=inner.Bottom-7f*uiScale;
            handle.DrawRect(new UIBox2(inner.Left+innerRadius,y,inner.Right-innerRadius,y+4f*uiScale),AccentColor);
            handle.DrawRect(new UIBox2(inner.Left+innerRadius,y+4f*uiScale,inner.Right-innerRadius,y+7f*uiScale),RustColor);
        }

        if (ShowHazardStripe)
            DrawHazardStripe(handle, box, uiScale);

        if (ShowWear)
            DrawWear(handle, box, uiScale);

        if (ShowBlood)
            DrawBlood(handle, box, uiScale);
        if (ShowGear)
            DrawGear(handle, box, uiScale);

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
            handle.DrawCircle(position, MathF.Max(1f,rivet*.42f),RivetColor,true);
            handle.DrawCircle(position,MathF.Max(.7f,rivet*.18f),new Color(45,39,32),true);
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
        var w = box.Width;
        var h = box.Height;
        // Layered cast-metal panels and recessed shadow wells.
        handle.DrawRect(new UIBox2(box.Left+w*.035f,box.Top+h*.10f,box.Right-w*.035f,box.Bottom-h*.075f),new Color(20,26,26));
        handle.DrawRect(new UIBox2(box.Left+w*.055f,box.Top+h*.12f,box.Right-w*.055f,box.Bottom-h*.095f),new Color(39,47,44));
        handle.DrawRect(new UIBox2(box.Left+w*.065f,box.Top+h*.14f,box.Right-w*.065f,box.Bottom-h*.115f),new Color(29,36,35));
        handle.DrawRect(new UIBox2(box.Left+w*.08f,box.Top+h*.20f,box.Left+w*.42f,box.Top+h*.43f),new Color(24,31,30));
        handle.DrawRect(new UIBox2(box.Left+w*.58f,box.Top+h*.54f,box.Left+w*.91f,box.Bottom-h*.19f),new Color(22,28,28));
        handle.DrawRect(new UIBox2(box.Left+w*.11f,box.Top+h*.72f,box.Left+w*.38f,box.Bottom-h*.16f),new Color(33,37,33));

        var rustPatches = new (float X,float Y,float W,float H)[]
        {
            (.07f,.12f,.10f,.065f),(.83f,.17f,.095f,.08f),(.12f,.83f,.13f,.07f),(.78f,.78f,.15f,.09f),(.46f,.91f,.09f,.035f)
        };
        foreach(var p in rustPatches)
        {
            var x=box.Left+w*p.X; var y=box.Top+h*p.Y; var pw=w*p.W; var ph=h*p.H;
            handle.DrawRect(new UIBox2(x-2*scale,y-2*scale,x+pw+2*scale,y+ph+2*scale),new Color(48,29,23));
            handle.DrawRect(new UIBox2(x,y,x+pw,y+ph),new Color(103,52,32));
            handle.DrawRect(new UIBox2(x+pw*.18f,y+ph*.22f,x+pw*.72f,y+ph*.72f),new Color(132,75,42));
        }
        var chips = new (float X,float Y,float W,float H)[]
        {
            (.20f,.26f,.035f,.012f),(.73f,.31f,.045f,.014f),(.52f,.67f,.055f,.012f),(.29f,.79f,.028f,.018f),(.87f,.61f,.022f,.015f),(.42f,.18f,.03f,.012f)
        };
        foreach(var p in chips)
        {
            var x=box.Left+w*p.X; var y=box.Top+h*p.Y;
            handle.DrawRect(new UIBox2(x,y,x+w*p.W,y+h*p.H),new Color(12,17,17));
            handle.DrawRect(new UIBox2(x+scale,y,x+w*p.W,y+h*p.H*.55f),new Color(145,146,127));
        }
        handle.DrawRect(new UIBox2(box.Left+w*.07f,box.Top+h*.135f,box.Left+w*.42f,box.Top+h*.15f),new Color(67,76,69));
        handle.DrawRect(new UIBox2(box.Left+w*.59f,box.Bottom-h*.13f,box.Right-w*.08f,box.Bottom-h*.105f),new Color(14,20,20));
    }

    private void DrawGear(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var radius=MathF.Min(12f*scale,MathF.Min(box.Width,box.Height)*.12f);
        if(radius<5f) return;
        var center=new Vector2(box.Right-radius*2.25f,box.Top+radius*2.1f);
        var toothWidth=radius*.48f;
        for(var i=0;i<8;i++)
        {
            var angle=MathF.Tau*i/8f;
            var d=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
            var p=center+d*(radius+radius*.16f);
            var tooth=new UIBox2(p.X-toothWidth*.5f,p.Y-toothWidth*.5f,p.X+toothWidth*.5f,p.Y+toothWidth*.5f);
            handle.DrawRect(tooth,new Color(22,27,26));
            handle.DrawRect(new UIBox2(tooth.Left+scale,tooth.Top+scale,tooth.Right-scale,tooth.Bottom-scale),new Color(116,91,61));
        }
        handle.DrawCircle(center+new Vector2(1.5f,2f)*scale,radius+2f*scale,new Color(12,16,16),true);
        handle.DrawCircle(center,radius+scale,new Color(53,43,32),true);
        handle.DrawCircle(center,radius*.78f,new Color(151,119,76),true);
        handle.DrawCircle(center,radius*.57f,new Color(34,39,37),true);
        handle.DrawCircle(center,radius*.28f,new Color(128,92,54),true);
        handle.DrawCircle(center,radius*.12f,new Color(19,23,22),true);
    }

    private void DrawWear(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var patches=new (float X,float Y,float W,float H)[]
        {
            (.015f,.18f,.045f,.13f),(.94f,.26f,.04f,.17f),(.11f,.94f,.16f,.035f),(.68f,.955f,.19f,.025f),(.86f,.06f,.10f,.035f)
        };
        foreach(var p in patches)
        {
            var x=box.Left+box.Width*p.X; var y=box.Top+box.Height*p.Y;
            var w=box.Width*p.W; var h=box.Height*p.H;
            handle.DrawRect(new UIBox2(x,y,x+w,y+h),new Color(51,29,23));
            handle.DrawRect(new UIBox2(x+scale,y+scale,x+w*.78f,y+h*.66f),RustColor);
            handle.DrawRect(new UIBox2(x+scale,y+scale,x+w*.58f,y+h*.34f),new Color(164,91,48));
        }
    }

    private void DrawBlood(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var stains=new (float X,float Y,float W,float H)[]
        {
            (.78f,.035f,.12f,.10f),(.08f,.82f,.14f,.08f),(.88f,.72f,.075f,.12f)
        };
        foreach(var p in stains)
        {
            var x=box.Left+box.Width*p.X; var y=box.Top+box.Height*p.Y;
            var w=box.Width*p.W; var h=box.Height*p.H;
            handle.DrawRect(new UIBox2(x-2*scale,y-2*scale,x+w+2*scale,y+h+2*scale),new Color(43,15,17));
            handle.DrawRect(new UIBox2(x,y,x+w,y+h),new Color(75,17,22));
            handle.DrawRect(new UIBox2(x+w*.16f,y+h*.18f,x+w*.7f,y+h*.72f),BloodColor);
        }
    }

    private void DrawHazardStripe(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var y=box.Bottom-9f*scale;
        var start=box.Left+20f*scale;
        var end=box.Right-20f*scale;
        handle.DrawRect(new UIBox2(start,y,end,y+7f*scale),AccentColor);
        for(var x=start;x<end;x+=18f*scale)
            handle.DrawRect(new UIBox2(x,y,MathF.Min(x+6f*scale,end),y+7f*scale),RivetColor);
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
