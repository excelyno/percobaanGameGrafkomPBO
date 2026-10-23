using System.Drawing;
using System.Drawing.Drawing2D;

namespace FarmGame.Core;

/// <summary>
/// Kumpulan helper grafika (konsep ala Cairo: path, gradient, alpha).
/// Static utility class — tidak perlu diinstansiasi.
/// </summary>
public static class Gfx
{
    /// <summary>Interpolasi linear antara dua warna.</summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    /// <summary>Membuat GraphicsPath persegi panjang dengan sudut bulat.</summary>
    public static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>
    /// Cahaya lembut: radial gradient dari tengah (opaque) ke tepi (transparan).
    /// </summary>
    public static void Glow(Graphics g, float cx, float cy, float r, Color core, Color edge)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
        using var pb = new PathGradientBrush(path)
        {
            CenterColor = core,
            SurroundColors = new[] { edge }
        };
        g.FillPath(pb, path);
    }
}
