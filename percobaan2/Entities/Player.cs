using System.Drawing;
using System.Drawing.Drawing2D;
using FarmGame.Core;

namespace FarmGame.Entities;

// ============================================================
//  PLAYER — karakter vector (GDI+ procedural art)
// ============================================================

/// <summary>
/// Karakter pemain yang bisa bergerak dengan WASD atau gamepad.
/// Digambar sepenuhnya secara procedural (vector GDI+) — tidak butuh sprite.
/// </summary>
public class Player : GameObject, IRenderable
{
    // ---- Fisika ----
    public float VelX { get; private set; }
    public float VelY { get; private set; }

    private const float WalkSpeed   = 180f;
    private const float SprintSpeed = 320f;
    private const float Friction    = 12f;

    // ---- Animasi ----
    private float _walkCycle;   // 0..2π untuk osilasi kaki/tangan
    private float _bobY;        // bouncing naik-turun saat jalan

    // ---- Visual ----
    private FacingDir _facing = FacingDir.Down;
    private bool _isMoving;

    // Warna karakter (bisa dikustomisasi)
    public Color SkinColor  { get; set; } = Color.FromArgb(255, 220, 170);
    public Color ShirtColor { get; set; } = Color.FromArgb(70, 130, 200);
    public Color PantsColor { get; set; } = Color.FromArgb(50, 80, 140);
    public Color HatColor   { get; set; } = Color.FromArgb(180, 60, 40);

    private enum FacingDir { Up, Down, Left, Right }

    // ============================================================
    public void Update(InputState input, float dt)
    {
        float speed = input.Sprint ? SprintSpeed : WalkSpeed;

        // Percepatan dari input
        float ax = input.AxisX * speed;
        float ay = input.AxisY * speed;

        // Lerp velocity ke target (friction-based acceleration)
        VelX = Lerp(VelX, ax, Friction * dt);
        VelY = Lerp(VelY, ay, Friction * dt);

        // Snap ke nol jika sangat kecil
        if (MathF.Abs(VelX) < 1f) VelX = 0;
        if (MathF.Abs(VelY) < 1f) VelY = 0;

        // Gerakkan posisi
        SetPosition(X + VelX * dt, Y + VelY * dt);

        // Animasi
        _isMoving = VelX != 0 || VelY != 0;
        if (_isMoving)
        {
            float moveLen = MathF.Sqrt(VelX * VelX + VelY * VelY);
            _walkCycle += dt * moveLen / 40f; // kecepatan osilasi proporsional
            _bobY = MathF.Sin(_walkCycle * 2f) * 3f;

            // Update arah hadap berdasarkan kecepatan dominan
            if (MathF.Abs(VelX) > MathF.Abs(VelY))
                _facing = VelX > 0 ? FacingDir.Right : FacingDir.Left;
            else
                _facing = VelY > 0 ? FacingDir.Down : FacingDir.Up;
        }
        else
        {
            _bobY = Lerp(_bobY, 0f, 10f * dt);
        }
    }

    public void Draw(Graphics g, float time)
    {
        var state = g.Save();
        // Pivot di kaki karakter
        g.TranslateTransform(X, Y + _bobY);

        switch (_facing)
        {
            case FacingDir.Down:  DrawFront(g, time); break;
            case FacingDir.Up:    DrawBack(g, time);  break;
            case FacingDir.Left:  DrawSide(g, time, flipX: true);  break;
            case FacingDir.Right: DrawSide(g, time, flipX: false); break;
        }

        g.Restore(state);
    }

    // ---- View: Depan ----
    private void DrawFront(Graphics g, float time)
    {
        float legSwing = _isMoving ? MathF.Sin(_walkCycle) * 10f : 0f;
        float armSwing = _isMoving ? MathF.Sin(_walkCycle + MathF.PI) * 12f : 0f;

        // Kaki kiri & kanan
        DrawLeg(g, -6f,  0f, -legSwing);
        DrawLeg(g,  6f,  0f,  legSwing);

        // Badan
        using var bodyBr = new SolidBrush(ShirtColor);
        g.FillRectangle(bodyBr, -9f, -28f, 18f, 18f);

        // Lengan kiri & kanan
        DrawArm(g, -14f, -26f, -armSwing);
        DrawArm(g,  14f, -26f,  armSwing);

        // Kepala
        DrawHead(g, facing: FacingDir.Down);
    }

    // ---- View: Belakang ----
    private void DrawBack(Graphics g, float time)
    {
        float legSwing = _isMoving ? MathF.Sin(_walkCycle) * 10f : 0f;

        DrawLeg(g, -6f, 0f, -legSwing);
        DrawLeg(g,  6f, 0f,  legSwing);

        using var bodyBr = new SolidBrush(ShirtColor);
        g.FillRectangle(bodyBr, -9f, -28f, 18f, 18f);

        DrawArm(g, -14f, -26f, legSwing);
        DrawArm(g,  14f, -26f, -legSwing);

        DrawHead(g, facing: FacingDir.Up);
    }

    // ---- View: Samping ----
    private void DrawSide(Graphics g, float time, bool flipX)
    {
        float legSwing  = _isMoving ? MathF.Sin(_walkCycle) * 14f : 0f;
        float armSwingF = _isMoving ? MathF.Sin(_walkCycle) * 16f : 0f;
        float armSwingB = _isMoving ? MathF.Sin(_walkCycle + MathF.PI) * 10f : 0f;
        float flip = flipX ? -1f : 1f;

        // Kaki belakang (lebih redup)
        using (var legBack = new SolidBrush(Darken(PantsColor, 0.6f)))
        {
            var lstate = g.Save();
            g.TranslateTransform(flip * 3f, 0f);
            g.RotateTransform(-legSwing);
            g.FillRectangle(new SolidBrush(Darken(PantsColor, 0.6f)), -4f, -10f, 8f, 18f);
            g.Restore(lstate);
        }

        // Kaki depan
        {
            var lstate = g.Save();
            g.RotateTransform(legSwing);
            g.FillRectangle(new SolidBrush(PantsColor), -4f, -10f, 8f, 18f);
            // Sepatu
            g.FillEllipse(new SolidBrush(Color.FromArgb(40,30,25)), -5f*flip, 5f, 10f, 6f);
            g.Restore(lstate);
        }

        // Badan
        using var bodyBr = new SolidBrush(ShirtColor);
        g.FillRectangle(bodyBr, -7f, -28f, 14f, 18f);

        // Lengan belakang
        {
            var astate = g.Save();
            g.TranslateTransform(-flip * 4f, -24f);
            g.RotateTransform(-armSwingB);
            g.FillRectangle(new SolidBrush(Darken(ShirtColor, 0.7f)), -3f, 0f, 6f, 14f);
            g.Restore(astate);
        }

        // Lengan depan
        {
            var astate = g.Save();
            g.TranslateTransform(flip * 4f, -24f);
            g.RotateTransform(armSwingF);
            g.FillRectangle(new SolidBrush(ShirtColor), -3f, 0f, 6f, 14f);
            g.Restore(astate);
        }

        DrawHead(g, facing: flipX ? FacingDir.Left : FacingDir.Right);
    }

    // ---- Sub: Kepala ----
    private void DrawHead(Graphics g, FacingDir facing)
    {
        // Leher
        using var neckBr = new SolidBrush(SkinColor);
        g.FillRectangle(neckBr, -3f, -34f, 6f, 8f);

        // Kepala
        using var headBr = new SolidBrush(SkinColor);
        g.FillEllipse(headBr, -11f, -54f, 22f, 24f);

        // Topi
        using var hatBr  = new SolidBrush(HatColor);
        using var brimBr = new SolidBrush(Darken(HatColor, 0.8f));
        g.FillRectangle(hatBr,  -8f,  -68f, 16f, 16f);   // crown
        g.FillRectangle(brimBr, -13f, -54f, 26f,  5f);   // brim

        // Mata (hanya tampak dari depan/samping)
        if (facing != FacingDir.Up)
        {
            using var eyeBr = new SolidBrush(Color.FromArgb(40, 30, 20));
            if (facing == FacingDir.Down)
            {
                g.FillEllipse(eyeBr, -6f, -48f, 4f, 4f);
                g.FillEllipse(eyeBr,  2f, -48f, 4f, 4f);
            }
            else // samping
            {
                float ex = facing == FacingDir.Right ? 3f : -7f;
                g.FillEllipse(eyeBr, ex, -48f, 4f, 4f);
            }
        }
    }

    // ---- Sub: Kaki ----
    private void DrawLeg(Graphics g, float px, float py, float angle)
    {
        var state = g.Save();
        g.TranslateTransform(px, py - 10f);
        g.RotateTransform(angle);
        using var br = new SolidBrush(PantsColor);
        g.FillRectangle(br, -4f, 0f, 8f, 18f);
        // Sepatu
        using var shoe = new SolidBrush(Color.FromArgb(40, 30, 25));
        g.FillEllipse(shoe, -5f, 14f, 12f, 7f);
        g.Restore(state);
    }

    // ---- Sub: Lengan ----
    private void DrawArm(Graphics g, float px, float py, float angle)
    {
        var state = g.Save();
        g.TranslateTransform(px, py);
        g.RotateTransform(angle);
        using var br = new SolidBrush(ShirtColor);
        g.FillRectangle(br, -3f, 0f, 6f, 14f);
        using var skin = new SolidBrush(SkinColor);
        g.FillEllipse(skin, -3f, 12f, 7f, 7f);
        g.Restore(state);
    }

    // ---- Helpers ----
    private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);

    private static Color Darken(Color c, float factor) =>
        Color.FromArgb(c.A, (int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));

    /// <summary>Clamp posisi ke dalam batas dunia.</summary>
    public void ClampToWorld(float worldW, float worldH, float margin = 16f)
    {
        SetPosition(
            Math.Clamp(X, margin, worldW - margin),
            Math.Clamp(Y, margin, worldH - margin));
    }
}
