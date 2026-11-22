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
    public CombatPanelButton()
    {
        Label.FontColorOverride = new Color(218, 198, 155);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var size=PixelSizeBox.Size;
        if(size.X<=2||size.Y<=2) return;

        var active=HasStyleClass("CombatTabActive");
        var primary=HasStyleClass("CombatPrimary");
        var disabled=Disabled;
        var pressed=HasStylePseudoClass("pressed");
        var hovered=HasStylePseudoClass("hover");
        var dark=new Color(13,17,17);
        var rim=disabled?new Color(65,66,59):active?new Color(181,130,72):new Color(105,82,54);
        var plate=disabled?new Color(32,35,33):pressed?new Color(53,43,34):hovered?new Color(64,58,45):new Color(39,46,43);
        var steel=new Color(135,139,121);

        // Thick cast frame, recessed shadow and raised face: no one-pixel strokes.
        handle.DrawRect(new UIBox2(0,0,size.X,size.Y),dark);
        handle.DrawRect(new UIBox2(2,2,size.X-2,size.Y-2),new Color(57,47,36));
        handle.DrawRect(new UIBox2(3,3,size.X-3,size.Y-3),rim);
        handle.DrawRect(new UIBox2(5,5,size.X-5,size.Y-5),dark);
        handle.DrawRect(new UIBox2(6,6,size.X-6,size.Y-7),plate);
        handle.DrawRect(new UIBox2(7,6,size.X-7,MathF.Min(size.Y-7,10)),new Color(75,82,72));
        handle.DrawRect(new UIBox2(7,size.Y-8,size.X-7,size.Y-5),new Color(19,24,23));
        handle.DrawRect(new UIBox2(8,size.Y-5,size.X-8,size.Y-3),new Color(91,49,32));

        // Broad chipped-paint insets at the corners.
        var corner=MathF.Min(4f,size.Y*.16f);
        handle.DrawRect(new UIBox2(7,7,7+corner,7+corner),new Color(22,26,24));
        handle.DrawRect(new UIBox2(size.X-7-corner,7,size.X-7,7+corner),new Color(22,26,24));
        handle.DrawRect(new UIBox2(7,size.Y-7-corner,7+corner,size.Y-7),new Color(22,26,24));
        handle.DrawRect(new UIBox2(size.X-7-corner,size.Y-7-corner,size.X-7,size.Y-7),new Color(22,26,24));

        // Large fasteners with raised brass heads.
        var inset=MathF.Min(8f,MathF.Min(size.X,size.Y)*.22f);
        foreach(var p in new[]{new Vector2(inset,inset),new Vector2(size.X-inset,inset),new Vector2(inset,size.Y-inset),new Vector2(size.X-inset,size.Y-inset)})
        {
            handle.DrawCircle(p,3.8f,dark,true);
            handle.DrawCircle(p,2.8f,new Color(103,62,39),true);
            handle.DrawCircle(p,1.8f,steel,true);
            handle.DrawCircle(p,.75f,new Color(35,34,29),true);
        }

        // Primary controls carry a small raised gear boss instead of a decorative stripe.
        if((primary||active)&&size.X>72&&size.Y>24)
        {
            var center=new Vector2(size.X-17f,size.Y*.5f);
            for(var i=0;i<8;i++)
            {
                var angle=MathF.Tau*i/8f;
                var d=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
                var p=center+d*7.2f;
                handle.DrawRect(new UIBox2(p.X-2.2f,p.Y-2.2f,p.X+2.2f,p.Y+2.2f),new Color(22,26,24));
                handle.DrawRect(new UIBox2(p.X-1.4f,p.Y-1.4f,p.X+1.4f,p.Y+1.4f),rim);
            }
            handle.DrawCircle(center+new Vector2(1f,1.5f),7f,dark,true);
            handle.DrawCircle(center,5.8f,rim,true);
            handle.DrawCircle(center,3.7f,new Color(36,42,39),true);
            handle.DrawCircle(center,1.8f,steel,true);
        }

        Label.FontColorOverride=disabled?new Color(125,119,102):new Color(224,207,166);
    }
}
