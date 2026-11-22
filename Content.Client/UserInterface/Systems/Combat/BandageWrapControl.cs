// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Systems.Combat;

/// <summary>
/// Drag-driven bandage winding control. The roll starts at twelve o'clock and
/// advances only when the pointer follows the ring clockwise while LMB is held.
/// Releasing pauses the wrap; grabbing the roll again resumes from its current spot.
/// </summary>
public sealed class BandageWrapControl : Control
{
    private const float StartAngle = -MathF.PI / 2f;
    private const float Tau = MathF.PI * 2f;
    private const float TrackRadius = 82f;
    private const float GrabRadius = 22f;
    private const float TrackTolerance = 32f;
    private const float MaxStepRadians = 0.70f;

    private bool _dragging;
    private bool _completed;
    private float _progress;
    private float _lastAngle;

    public event Action<float>? ProgressChanged;
    public event Action? Completed;

    public float Progress => _progress;

    public BandageWrapControl()
    {
        MouseFilter = MouseFilterMode.Stop;
        DefaultCursorShape = CursorShape.Hand;
    }

    public void Reset()
    {
        _dragging = false;
        _completed = false;
        _progress = 0f;
        _lastAngle = StartAngle;
        UpdateDraw();
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick || args.State != BoundKeyState.Down || _completed)
            return;

        var pointer = args.RelativePixelPosition;
        var center = PixelSize / 2f;
        var roll = GetRollPosition(center, _progress);
        if (Vector2.DistanceSquared(pointer, roll) > GrabRadius * GrabRadius)
            return;

        _dragging = true;
        _lastAngle = GetAngle(pointer, center);
        UserInterfaceManager.ControlFocused = this;
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function != EngineKeyFunctions.UIClick || args.State != BoundKeyState.Up)
            return;

        _dragging = false;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (!_dragging || _completed)
            return;

        var center = PixelSize / 2f;
        var pointer = args.RelativePixelPosition;
        var offset = pointer - center;
        var distance = offset.Length();

        if (MathF.Abs(distance - TrackRadius) > TrackTolerance)
        {
            // Do not let a jump across the centre or outside the track create progress.
            _lastAngle = GetAngle(pointer, center);
            return;
        }

        var angle = GetAngle(pointer, center);
        var step = angle - _lastAngle;
        while (step > MathF.PI)
            step -= Tau;
        while (step < -MathF.PI)
            step += Tau;

        // Screen-space angles increase clockwise. Counter-clockwise movement is rejected.
        _lastAngle = angle;
        if (step <= 0f || step > MaxStepRadians)
            return;

        _progress = Math.Clamp(_progress + step / Tau, 0f, 1f);
        ProgressChanged?.Invoke(_progress);
        UpdateDraw();

        if (_progress < 1f)
            return;

        _completed = true;
        _dragging = false;
        Completed?.Invoke();
    }

    protected override void ControlFocusExited()
    {
        base.ControlFocusExited();
        _dragging = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var center = PixelSize / 2f;
        var radius = TrackRadius;

        // Recessed circular guide.
        handle.DrawCircle(center, radius + 12f, new Color(12, 16, 16), false);
        handle.DrawCircle(center, radius + 8f, new Color(116, 105, 80), false);
        handle.DrawCircle(center, radius, new Color(66, 76, 70), false);
        handle.DrawCircle(center, radius - 8f, new Color(23, 29, 28), false);
        handle.DrawCircle(center, radius - 1f, new Color(143, 132, 103), false);

        // The visible gauze is drawn as two parallel edges along the completed arc.
        var segments = Math.Max(1, (int) MathF.Ceiling(96f * _progress));
        var previous = GetPoint(center, radius, StartAngle);
        for (var i = 1; i <= segments; i++)
        {
            var progress = _progress * i / segments;
            var angle = StartAngle + Tau * progress;
            var current = GetPoint(center, radius, angle);
            var tangent = Vector2.Normalize(new Vector2(-(current - center).Y, (current - center).X));
            var halfWidth = 4f;
            handle.DrawLine(previous + tangent * halfWidth, current + tangent * halfWidth, new Color(226, 218, 191));
            handle.DrawLine(previous - tangent * halfWidth, current - tangent * halfWidth, new Color(226, 218, 191));
            handle.DrawLine(previous, current, new Color(191, 183, 158));
            previous = current;
        }

        // Small winding ticks make the clockwise direction legible.
        for (var i = 0; i < 12; i++)
        {
            var angle = StartAngle + Tau * i / 12f;
            var from = GetPoint(center, radius + 15f, angle);
            var to = GetPoint(center, radius + 20f, angle);
            handle.DrawLine(from, to, new Color(151, 139, 107));
        }

        // The twisted roll remains attached to the unwrapped end of the gauze.
        var roll = GetRollPosition(center, _progress);
        handle.DrawCircle(roll, 13f, new Color(17, 20, 19), true);
        handle.DrawCircle(roll, 10f, new Color(232, 224, 198), true);
        handle.DrawCircle(roll, 6f, new Color(190, 181, 153), true);
        handle.DrawCircle(roll, 2.5f, new Color(89, 91, 78), true);

        var rollAngle = StartAngle + Tau * _progress;
        var axis = new Vector2(MathF.Cos(rollAngle), MathF.Sin(rollAngle));
        handle.DrawLine(roll - axis * 7f, roll + axis * 7f, new Color(250, 244, 220));
    }

    private static float GetAngle(Vector2 point, Vector2 center)
    {
        var offset = point - center;
        return MathF.Atan2(offset.Y, offset.X);
    }

    private static Vector2 GetRollPosition(Vector2 center, float progress)
    {
        return GetPoint(center, TrackRadius, StartAngle + Tau * progress);
    }

    private static Vector2 GetPoint(Vector2 center, float radius, float angle)
    {
        return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }
}
