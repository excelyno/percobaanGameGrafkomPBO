
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using FarmGame.Core;

namespace FarmGame.Entities;

public class Player : GameObject, IRenderable
{
    public float VelX { get; private set; }
    public float VelY { get; private set; }

    public Color SkinColor { get; set; } = Color.FromArgb(255, 190, 145);
    public Color ShirtColor { get; set; } = Color.FromArgb(48, 112, 210);
    public Color PantsColor { get; set; } = Color.FromArgb(30, 52, 115);
    public Color HatColor { get; set; } = Color.FromArgb(190, 43, 57);
    public Color HairColor { get; set; } = Color.FromArgb(53, 37, 53);
    public Color ShoeColor { get; set; } = Color.FromArgb(111, 62, 48);
    public Color AccentColor { get; set; } = Color.FromArgb(255, 183, 78);

    private const float WalkSpeed = 180f;
    private const float SprintSpeed = 320f;
    private const float Acceleration = 1800f;
    private const float PixelScale = 4f;

    private float _walkCycle;
    private float _idleTime;
    private bool _isMoving;

    private FacingDir _facing = FacingDir.Down;

    private enum FacingDir { Up, Down, Left, Right }

    // Palet outline dan bayangan.
    private static readonly Color Outline = Color.FromArgb(25, 24, 43);
    private static readonly Color DeepShadow = Color.FromArgb(36, 30, 52);
    private static readonly Color HatShade = Color.FromArgb(112, 34, 52);
    private static readonly Color ScarfColor = Color.FromArgb(232, 54, 57);
    private static readonly Color BootSole = Color.FromArgb(49, 35, 45);
    private static readonly Color HairShade = Color.FromArgb(38, 30, 48);

    public void Update(InputState input, float dt)
    {
        if (dt <= 0f) return;
        dt = Math.Min(dt, 0.05f);

        float ix = Math.Clamp(input.AxisX, -1f, 1f);
        float iy = Math.Clamp(input.AxisY, -1f, 1f);

        float length = MathF.Sqrt(ix * ix + iy * iy);

        if (length > 1f)
        {
            ix /= length;
            iy /= length;
        }

        float speed = input.Sprint ? SprintSpeed : WalkSpeed;
        float tx = ix * speed;
        float ty = iy * speed;
        float maxDelta = Acceleration * dt;

        VelX = MoveTowards(VelX, tx, maxDelta);
        VelY = MoveTowards(VelY, ty, maxDelta);

        if (MathF.Abs(tx) < 0.01f && MathF.Abs(VelX) < 2f)
            VelX = 0f;

        if (MathF.Abs(ty) < 0.01f && MathF.Abs(VelY) < 2f)
            VelY = 0f;

        SetPosition(X + VelX * dt, Y + VelY * dt);

        _isMoving = MathF.Abs(VelX) > 2f || MathF.Abs(VelY) > 2f;

        if (_isMoving)
        {
            float magnitude = MathF.Sqrt(VelX * VelX + VelY * VelY);
            _walkCycle += dt * magnitude / 40f;

            if (MathF.Abs(VelX) > MathF.Abs(VelY))
                _facing = VelX > 0 ? FacingDir.Right : FacingDir.Left;
            else if (MathF.Abs(VelY) > 0.01f)
                _facing = VelY > 0 ? FacingDir.Down : FacingDir.Up;

            _idleTime = 0f;
        }
        else
        {
            _idleTime += dt;
        }
    }

    public void Draw(Graphics g, float time)
    {
        GraphicsState state = g.Save();

        try
        {
            // Snap posisi ke grid agar pixel tidak terlihat bergetar.
            float sx = MathF.Round(X / PixelScale) * PixelScale;
            float sy = MathF.Round(Y / PixelScale) * PixelScale;

            g.TranslateTransform(sx, sy);
            g.ScaleTransform(PixelScale, PixelScale);

            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.CompositingQuality = CompositingQuality.HighSpeed;

            // Bayangan tanah.
            Rect(g, Color.FromArgb(65, 15, 18, 35), -10, -1, 20, 3);

            // Napas saat idle, bob kecil saat berjalan.
            float bob = _isMoving
                ? MathF.Round(MathF.Sin(_walkCycle * 2f))
                : MathF.Round(MathF.Sin(_idleTime * 2.5f) * 0.25f);

            g.TranslateTransform(0f, bob);

            switch (_facing)
            {
                case FacingDir.Down:
                    DrawFront(g);
                    break;
                case FacingDir.Up:
                    DrawBack(g);
                    break;
                case FacingDir.Left:
                    DrawSide(g, true);
                    break;
                case FacingDir.Right:
                    DrawSide(g, false);
                    break;
            }
        }
        finally
        {
            g.Restore(state);
        }
    }

    // ---------------------------------------------------------
    // DEPAN
    // ---------------------------------------------------------

    private void DrawFront(Graphics g)
    {
        int step = _isMoving
            ? (int)MathF.Round(MathF.Sin(_walkCycle) * 2f)
            : 0;

        // Kaki belakang dan depan.
        DrawLeg(g, -4, step);
        DrawLeg(g, 4, -step);

        // Lengan di belakang badan.
        DrawArm(g, -8, -27, -step, true);
        DrawArm(g, 8, -27, step, false);

        DrawBody(g);
        DrawOveralls(g, false);
        DrawScarf(g, false);
        DrawHead(g, FacingDir.Down);
        DrawHat(g);
    }

    // ---------------------------------------------------------
    // BELAKANG
    // ---------------------------------------------------------

    private void DrawBack(Graphics g)
    {
        int step = _isMoving
            ? (int)MathF.Round(MathF.Sin(_walkCycle) * 2f)
            : 0;

        DrawLeg(g, -4, step);
        DrawLeg(g, 4, -step);

        DrawArm(g, -8, -27, step, true);
        DrawArm(g, 8, -27, -step, false);

        DrawBody(g);
        DrawOveralls(g, true);

        // Rambut belakang.
        Rect(g, Outline, -7, -39, 14, 8);
        Rect(g, HairColor, -6, -39, 12, 7);
        Rect(g, HairShade, -7, -34, 14, 3);

        // Tali ransel.
        Rect(g, Outline, -5, -34, 10, 10);
        Rect(g, ShoeColor, -4, -33, 8, 8);
        Rect(g, AccentColor, -3, -32, 6, 1);

        DrawScarf(g, true);
        DrawHead(g, FacingDir.Up);
        DrawHat(g);
    }

    // ---------------------------------------------------------
    // SAMPING
    // ---------------------------------------------------------

    private void DrawSide(Graphics g, bool left)
    {
        GraphicsState state = g.Save();

        try
        {
            if (left)
                g.ScaleTransform(-1f, 1f);

            int step = _isMoving
                ? (int)MathF.Round(MathF.Sin(_walkCycle) * 2f)
                : 0;

            DrawLeg(g, -2, -step);
            DrawLeg(g, 3, step);

            DrawArm(g, -4, -27, -step, true);

            DrawBody(g);
            DrawOveralls(g, false);
            DrawScarf(g, false);

            DrawArm(g, 5, -27, step, false);

            // Rambut di belakang kepala.
            Rect(g, Outline, -5, -40, 12, 9);
            Rect(g, HairColor, -4, -39, 10, 7);

            // Hidung kecil sebagai penanda arah.
            DrawHead(g, FacingDir.Right);
            DrawHat(g);
        }
        finally
        {
            g.Restore(state);
        }
    }

    // ---------------------------------------------------------
    // BADAN
    // ---------------------------------------------------------

    private void DrawBody(Graphics g)
    {
        // Siluet badan lebih lebar daripada versi sebelumnya.
        Rect(g, Outline, -7, -31, 14, 13);
        Rect(g, ShirtColor, -6, -30, 12, 11);

        // Highlight bahu dan lengan baju.
        Rect(g, Lighten(ShirtColor, 0.35f), -6, -30, 3, 4);
        Rect(g, Darken(ShirtColor, 0.7f), 3, -29, 3, 7);

        // Leher.
        Rect(g, Outline, -3, -35, 6, 6);
        Rect(g, SkinColor, -2, -34, 4, 5);
    }

    private void DrawOveralls(Graphics g, bool back)
    {
        // Overall bib dan bagian celana.
        Rect(g, Outline, -7, -23, 14, 12);
        Rect(g, PantsColor, -6, -22, 12, 10);

        // Bagian bib.
        Rect(g, Outline, -5, -30, 10, 10);
        Rect(g, Darken(PantsColor, 0.8f), -4, -29, 8, 8);
        Rect(g, PantsColor, -3, -28, 6, 6);

        // Tali bahu.
        Rect(g, Outline, -5, -31, 3, 7);
        Rect(g, Outline, 2, -31, 3, 7);
        Rect(g, ShirtColor, -4, -30, 1, 5);
        Rect(g, ShirtColor, 3, -30, 1, 5);

        if (!back)
        {
            // Kantong depan.
            Rect(g, Outline, -3, -26, 6, 5);
            Rect(g, Lighten(PantsColor, 0.18f), -2, -25, 4, 3);

            Rect(g, AccentColor, -4, -29, 2, 2);
            Rect(g, AccentColor, 2, -29, 2, 2);

            Rect(g, AccentColor, -1, -24, 2, 1);
        }
        else
        {
            Rect(g, Darken(PantsColor, 0.7f), -3, -20, 6, 3);
            Rect(g, Lighten(PantsColor, 0.15f), -2, -20, 4, 1);
        }
    }

    // ---------------------------------------------------------
    // SYAL
    // ---------------------------------------------------------

    private void DrawScarf(Graphics g, bool back)
    {
        Rect(g, HatShade, -5, -35, 10, 4);
        Rect(g, ScarfColor, -4, -35, 8, 3);
        Rect(g, Lighten(ScarfColor, 0.25f), -4, -35, 4, 1);

        if (!back)
        {
            // Ujung syal turun di depan dada.
            Rect(g, HatShade, 1, -33, 4, 7);
            Rect(g, ScarfColor, 2, -32, 2, 5);
            Rect(g, Lighten(ScarfColor, 0.2f), 2, -32, 1, 2);
        }
    }

    // ---------------------------------------------------------
    // KEPALA DAN WAJAH
    // ---------------------------------------------------------

    private void DrawHead(Graphics g, FacingDir facing)
    {
        // Kepala besar dan rambut berlapis.
        Rect(g, Outline, -8, -45, 16, 12);
        Rect(g, HairColor, -7, -44, 14, 11);

        Rect(g, Outline, -8, -44, 16, 8);
        Rect(g, SkinColor, -7, -43, 14, 9);

        // Bayangan sisi wajah.
        Rect(g, Darken(SkinColor, 0.82f), -7, -37, 2, 2);
        Rect(g, Lighten(SkinColor, 0.22f), -5, -42, 4, 2);

        // Poni.
        Rect(g, HairShade, -7, -44, 14, 3);
        Rect(g, HairColor, -6, -44, 12, 2);
        Rect(g, HairColor, -5, -42, 4, 2);
        Rect(g, HairColor, 2, -42, 4, 1);

        if (facing == FacingDir.Up)
        {
            Rect(g, HairShade, -7, -38, 14, 3);
            return;
        }

        if (facing == FacingDir.Down)
        {
            DrawEye(g, -4, -39);
            DrawEye(g, 2, -39);

            // Pipi.
            Rect(g, Color.FromArgb(190, 112, 91), -6, -36, 2, 1);
            Rect(g, Color.FromArgb(190, 112, 91), 4, -36, 2, 1);

            // Mulut.
            Rect(g, HatShade, -1, -35, 2, 1);
        }
        else
        {
            DrawEye(g, 3, -39);
            Rect(g, SkinColor, 7, -37, 2, 2);
        }
    }

    private static void DrawEye(Graphics g, int x, int y)
    {
        Rect(g, Outline, x, y, 3, 4);
        Rect(g, Color.FromArgb(24, 28, 48), x, y, 2, 3);
        Rect(g, Color.White, x, y, 1, 1);
    }

    // ---------------------------------------------------------
    // TOPI PETANI
    // ---------------------------------------------------------

    private void DrawHat(Graphics g)
    {
        // Brim lebar: ini yang membuat siluet topi lebih khas.
        Rect(g, Outline, -10, -47, 20, 4);
        Rect(g, HatShade, -9, -47, 18, 3);
        Rect(g, HatColor, -8, -48, 16, 2);

        // Crown.
        Rect(g, Outline, -7, -54, 14, 9);
        Rect(g, HatShade, -6, -53, 12, 7);
        Rect(g, HatColor, -5, -53, 10, 6);

        // Highlight.
        Rect(g, Lighten(HatColor, 0.3f), -4, -52, 6, 1);
        Rect(g, Lighten(HatColor, 0.18f), -5, -51, 2, 2);

        // Pita topi.
        Rect(g, Outline, -6, -48, 12, 2);
        Rect(g, HatShade, -5, -48, 10, 1);
        Rect(g, ScarfColor, -4, -48, 8, 1);

        // Pinggiran bawah.
        Rect(g, DeepShadow, -9, -45, 18, 1);
    }

    // ---------------------------------------------------------
    // KAKI DAN SEPATU
    // ---------------------------------------------------------

    private void DrawLeg(Graphics g, int x, int swing)
    {
        GraphicsState state = g.Save();

        try
        {
            g.TranslateTransform(x, 0);
            g.TranslateTransform(swing, 0);

            // Celana pendek ke arah sepatu.
            Rect(g, Outline, -4, -14, 8, 8);
            Rect(g, PantsColor, -3, -13, 6, 7);
            Rect(g, Lighten(PantsColor, 0.18f), -3, -13, 2, 3);

            // Sepatu tebal.
            Rect(g, BootSole, -5, -7, 11, 6);
            Rect(g, Outline, -5, -8, 11, 6);
            Rect(g, ShoeColor, -4, -8, 9, 4);
            Rect(g, Lighten(ShoeColor, 0.25f), -3, -8, 4, 1);
            Rect(g, Darken(ShoeColor, 0.7f), 2, -6, 3, 2);
        }
        finally
        {
            g.Restore(state);
        }
    }

    // ---------------------------------------------------------
    // LENGAN
    // ---------------------------------------------------------

    private void DrawArm(
        Graphics g,
        int x,
        int y,
        int swing,
        bool back)
    {
        GraphicsState state = g.Save();

        try
        {
            g.TranslateTransform(x, y);
            g.TranslateTransform(0, swing);

            Color sleeve = back
                ? Darken(ShirtColor, 0.72f)
                : ShirtColor;

            Rect(g, Outline, -3, 0, 6, 10);
            Rect(g, sleeve, -2, 1, 4, 8);
            Rect(g, Lighten(sleeve, 0.22f), -2, 1, 2, 3);

            // Manset.
            Rect(g, Darken(sleeve, 0.7f), -2, 7, 4, 2);

            // Tangan.
            Rect(g, Outline, -3, 9, 6, 5);
            Rect(g, SkinColor, -2, 9, 4, 4);
            Rect(g, Lighten(SkinColor, 0.2f), -2, 9, 2, 1);
        }
        finally
        {
            g.Restore(state);
        }
    }

    // ---------------------------------------------------------
    // UTILITAS
    // ---------------------------------------------------------

    private static void Rect(
        Graphics g,
        Color color,
        int x,
        int y,
        int width,
        int height)
    {
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, x, y, width, height);
    }

    private static float MoveTowards(
        float current,
        float target,
        float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current +
            MathF.Sign(target - current) * maxDelta;
    }

    private static Color Darken(Color c, float factor)
    {
        factor = Math.Clamp(factor, 0f, 1f);

        return Color.FromArgb(
            c.A,
            (int)(c.R * factor),
            (int)(c.G * factor),
            (int)(c.B * factor)
        );
    }

    private static Color Lighten(Color c, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);

        return Color.FromArgb(
            c.A,
            (int)(c.R + (255 - c.R) * amount),
            (int)(c.G + (255 - c.G) * amount),
            (int)(c.B + (255 - c.B) * amount)
        );
    }

    public void ClampToWorld(
        float worldW,
        float worldH,
        float margin = 16f)
    {
        margin = Math.Max(0f, margin);

        SetPosition(
            Math.Clamp(X, margin, Math.Max(margin, worldW - margin)),
            Math.Clamp(Y, margin, Math.Max(margin, worldH - margin))
        );
    }
}
