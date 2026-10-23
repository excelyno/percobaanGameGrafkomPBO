using System.Drawing;

namespace FarmGame.Systems;

// ============================================================
//  SISTEM HUJAN (partikel + alpha blending)
// ============================================================

/// <summary>
/// Sistem partikel hujan.
/// Mengelola 220 tetesan air yang jatuh secara simulasi sederhana.
/// </summary>
public class Rain
{
    private class Drop { public float X, Y, Speed; }

    private readonly List<Drop> _drops = new();
    private readonly Random _rng = new();
    private readonly int _w, _h;

    /// <summary>Aktifkan atau nonaktifkan hujan.</summary>
    public bool Active { get; set; }

    public Rain(int width, int height)
    {
        _w = width;
        _h = height;
        for (int i = 0; i < 220; i++)
            _drops.Add(new Drop
            {
                X = _rng.Next(width + 100),
                Y = _rng.Next(height),
                Speed = 500 + _rng.Next(300)
            });
    }

    public void Update(float dt)
    {
        if (!Active) return;
        foreach (var d in _drops)
        {
            d.Y += d.Speed * dt;
            d.X -= d.Speed * 0.15f * dt;
            if (d.Y > _h)
            {
                d.Y = -10;
                d.X = _rng.Next(_w + 100);
            }
        }
    }

    public void Draw(Graphics g)
    {
        if (!Active) return;
        using var pen = new Pen(Color.FromArgb(130, 190, 210, 255), 1.5f);
        foreach (var d in _drops)
            g.DrawLine(pen, d.X, d.Y, d.X + 2f, d.Y - 14f);
    }
}
