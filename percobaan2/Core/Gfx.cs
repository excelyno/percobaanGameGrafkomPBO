
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FarmGame.Core;

/// <summary>
/// Kumpulan helper grafika untuk FarmGame.
/// Mendukung interpolasi warna, rounded rectangle, dan glow effect.
/// Semua method bersifat static sehingga tidak perlu membuat instance.
/// </summary>
public static class Gfx
{
    /// <summary>
    /// Menginterpolasi dua warna berdasarkan nilai t dari 0 sampai 1.
    /// t = 0 menghasilkan warna a, sedangkan t = 1 menghasilkan warna b.
    /// </summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        return Color.FromArgb(
            Interpolate(a.A, b.A, t),
            Interpolate(a.R, b.R, t),
            Interpolate(a.G, b.G, t),
            Interpolate(a.B, b.B, t)
        );
    }

    private static int Interpolate(int a, int b, float t)
    {
        return (int)Math.Round(a + (b - a) * t);
    }

    /// <summary>
    /// Membuat GraphicsPath berbentuk persegi panjang dengan sudut membulat.
    /// Radius otomatis dibatasi agar tidak melebihi setengah ukuran terkecil.
    /// Pemanggil bertanggung jawab membuang GraphicsPath setelah digunakan.
    /// </summary>
    public static GraphicsPath RoundRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();

        if (rect.Width <= 0 || rect.Height <= 0)
            return path;

        radius = Math.Clamp(
            radius,
            0f,
            Math.Min(rect.Width, rect.Height) / 2f
        );

        if (radius <= 0f)
        {
            path.AddRectangle(rect);
            return path;
        }

        float diameter = radius * 2f;

        path.AddArc(
            rect.X, rect.Y,
            diameter, diameter,
            180f, 90f
        );

        path.AddArc(
            rect.Right - diameter, rect.Y,
            diameter, diameter,
            270f, 90f
        );

        path.AddArc(
            rect.Right - diameter,
            rect.Bottom - diameter,
            diameter, diameter,
            0f, 90f
        );

        path.AddArc(
            rect.X,
            rect.Bottom - diameter,
            diameter, diameter,
            90f, 90f
        );

        path.CloseFigure();

        return path;
    }

    /// <summary>
    /// Menggambar glow berbentuk radial dengan warna inti dan tepi.
    /// Gunakan alpha pada warna untuk mengontrol transparansi.
    /// Radius harus lebih besar dari nol.
    /// </summary>
    public static void Glow(
        Graphics graphics,
        float centerX,
        float centerY,
        float radius,
        Color core,
        Color edge)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        if (radius <= 0f)
            return;

        using var path = new GraphicsPath();

        path.AddEllipse(
            centerX - radius,
            centerY - radius,
            radius * 2f,
            radius * 2f
        );

        using var brush = new PathGradientBrush(path)
        {
            CenterColor = core,
            SurroundColors = new[] { edge }
        };

        graphics.FillEllipse(
            brush,
            centerX - radius,
            centerY - radius,
            radius * 2f,
            radius * 2f
        );
    }
}
