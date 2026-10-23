using System.Drawing;
using System.Drawing.Drawing2D;
using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Systems;

// ============================================================
//  MINIGAME PANEN: tekan SPACE saat kursor di zona hijau
// ============================================================

/// <summary>
/// Minigame panen berbasis timing.
/// Pemain menekan SPACE saat kursor bergerak melewati zona hijau.
/// Hasil: PERFECT (x2), Bagus (x1.5), atau Meleset (x0.5).
/// </summary>
public class HarvestMinigame
{
    private static readonly Random Rng = new();

    private float _cursor;
    private float _dir = 1f;
    private readonly float _speed;
    private readonly float _center;
    private readonly float _width = 0.2f;
    private float _resultTime;

    public Tile Target { get; }
    public bool Finished { get; private set; }

    /// <summary>True ketika animasi hasil selesai ditampilkan dan minigame bisa di-dismiss.</summary>
    public bool Done => Finished && _resultTime > 0.9f;

    public float Multiplier { get; private set; } = 1f;
    public string ResultText { get; private set; } = "";

    public HarvestMinigame(Tile target)
    {
        Target = target;
        _center = 0.25f + (float)Rng.NextDouble() * 0.5f;
        _speed = 0.9f + (float)Rng.NextDouble() * 0.7f;
    }

    public void Update(float dt)
    {
        if (Finished) { _resultTime += dt; return; }

        _cursor += _dir * _speed * dt;
        if (_cursor > 1f) { _cursor = 1f; _dir = -1f; }
        if (_cursor < 0f) { _cursor = 0f; _dir = 1f; }
    }

    /// <summary>Pemain menekan SPACE — hitung skor berdasarkan posisi kursor.</summary>
    public void Stop()
    {
        if (Finished) return;
        float d = Math.Abs(_cursor - _center);

        if (d < _width * 0.2f) { Multiplier = 2f;   ResultText = "PERFECT!  x2"; }
        else if (d < _width * 0.5f) { Multiplier = 1.5f; ResultText = "Bagus!  x1.5"; }
        else { Multiplier = 0.5f; ResultText = "Meleset...  x0.5"; }

        Finished = true;
    }

    public void Draw(Graphics g, Rectangle b)
    {
        using (var panel = Gfx.RoundRect(b, 16))
        using (var fill = new SolidBrush(Color.FromArgb(235, 45, 32, 22)))
        using (var border = new Pen(Color.Goldenrod, 3f))
        {
            g.FillPath(fill, panel);
            g.DrawPath(border, panel);
        }

        using var font = new Font("Segoe UI", 13f, FontStyle.Bold);
        using var sf = new StringFormat { Alignment = StringAlignment.Center };
        string title = Finished ? ResultText : "Tekan SPACE saat kursor di zona hijau!";
        g.DrawString(title, font, Brushes.White, new RectangleF(b.X, b.Y + 14, b.Width, 30), sf);

        var bar = new RectangleF(b.X + 30, b.Y + 70, b.Width - 60, 26);
        using (var barPath = Gfx.RoundRect(bar, 10))
        using (var barFill = new SolidBrush(Color.FromArgb(70, 70, 75)))
            g.FillPath(barFill, barPath);

        // Zona hijau (gradient horizontal)
        float zx = bar.X + (_center - _width / 2f) * bar.Width;
        float zw = _width * bar.Width;
        using (var zone = new LinearGradientBrush(new RectangleF(zx, bar.Y, zw, bar.Height),
                   Color.FromArgb(60, 170, 70), Color.FromArgb(140, 230, 110), 0f))
            g.FillRectangle(zone, zx, bar.Y, zw, bar.Height);

        // Zona perfect (emas)
        float pw = zw * 0.4f;
        using (var gold = new SolidBrush(Color.Gold))
            g.FillRectangle(gold, bar.X + _center * bar.Width - pw / 2f, bar.Y, pw, bar.Height);

        // Kursor
        float cx = bar.X + _cursor * bar.Width;
        using var cp = new Pen(Color.White, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(cp, cx, bar.Y - 8, cx, bar.Bottom + 8);
        g.FillPolygon(Brushes.White, new[]
        {
            new PointF(cx - 7, bar.Y - 16), new PointF(cx + 7, bar.Y - 16), new PointF(cx, bar.Y - 6)
        });
    }
}
