using System.Drawing;

namespace FarmGame.Core;

// ============================================================
//  CAMERA — follow player, clamped ke batas dunia
// ============================================================

/// <summary>
/// Kamera 2D yang mengikuti target (player) dengan efek smooth lerp.
/// Transformasi kamera diterapkan ke Graphics sebelum menggambar dunia.
/// </summary>
public class Camera
{
    // Posisi tengah kamera dalam world-space
    public float X { get; private set; }
    public float Y { get; private set; }

    // Ukuran viewport (piksel layar)
    public int ViewW { get; }
    public int ViewH { get; }

    // Batas dunia (dalam piksel world)
    public int WorldW { get; set; }
    public int WorldH { get; set; }

    // Kecepatan lerp (0 = tetap, 1 = snap langsung)
    public float Smoothing { get; set; } = 8f;

    // Zoom (1.0 = normal, >1 = zoom in, <1 = zoom out)
    public float Zoom { get; set; } = 1f;

    public Camera(int viewW, int viewH, int worldW, int worldH)
    {
        ViewW  = viewW;
        ViewH  = viewH;
        WorldW = worldW;
        WorldH = worldH;
    }

    /// <summary>
    /// Update posisi kamera menuju target, dengan smooth lerp.
    /// </summary>
    public void Follow(float targetX, float targetY, float dt)
    {
        float t = Math.Clamp(Smoothing * dt, 0f, 1f);
        X = X + (targetX - X) * t;
        Y = Y + (targetY - Y) * t;
        ClampToBounds();
    }

    /// <summary>Snap langsung ke posisi tanpa lerp.</summary>
    public void SnapTo(float x, float y)
    {
        X = x; Y = y;
        ClampToBounds();
    }

    private void ClampToBounds()
    {
        float halfW = (ViewW / Zoom) / 2f;
        float halfH = (ViewH / Zoom) / 2f;
        X = Math.Clamp(X, halfW, WorldW - halfW);
        Y = Math.Clamp(Y, halfH, WorldH - halfH);
    }

    /// <summary>
    /// Terapkan transformasi kamera ke Graphics.
    /// Panggil sebelum menggambar elemen world-space.
    /// </summary>
    public void ApplyTransform(System.Drawing.Graphics g)
    {
        g.ResetTransform();
        g.ScaleTransform(Zoom, Zoom);
        g.TranslateTransform(
            ViewW / Zoom / 2f - X,
            ViewH / Zoom / 2f - Y);
    }

    /// <summary>
    /// Konversi posisi layar (screen) ke posisi dunia (world).
    /// Berguna untuk klik mouse pada build mode.
    /// </summary>
    public PointF ScreenToWorld(Point screen)
    {
        return new PointF(
            (screen.X / Zoom) - (ViewW / Zoom / 2f - X),
            (screen.Y / Zoom) - (ViewH / Zoom / 2f - Y));
    }

    /// <summary>
    /// Konversi posisi dunia ke layar.
    /// </summary>
    public PointF WorldToScreen(float wx, float wy)
    {
        return new PointF(
            (wx - X + ViewW / Zoom / 2f) * Zoom,
            (wy - Y + ViewH / Zoom / 2f) * Zoom);
    }
}
