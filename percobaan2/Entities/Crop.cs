using System.Drawing;
using System.Drawing.Drawing2D;
using FarmGame.Core;

namespace FarmGame.Entities;

// ============================================================
//  TANAMAN: Abstraction + Inheritance + Polymorphism
// ============================================================

/// <summary>
/// Base class abstrak untuk semua jenis tanaman.
/// Menggabungkan GameObject (posisi) dan IRenderable (gambar).
/// </summary>
public abstract class Crop : GameObject, IRenderable
{
    private int _daysGrown; // Encapsulation: tidak bisa diubah dari luar

    public abstract string Name { get; }
    public abstract int GrowthDays { get; }
    public abstract int SellPrice { get; }

    public float Growth => Math.Min(1f, (float)_daysGrown / GrowthDays);
    public bool IsRipe => _daysGrown >= GrowthDays;

    /// <summary>Dipanggil setiap pergantian hari. Tanaman tumbuh hanya jika disiram.</summary>
    public void OnDayPassed(bool watered)
    {
        if (watered && !IsRipe) _daysGrown++;
    }

    public void Draw(Graphics g, float time)
    {
        var state = g.Save();
        g.TranslateTransform(X, Y);

        // Affine transform: goyang tertiup angin (pivot di pangkal tanaman)
        float sway = (float)Math.Sin(time * 2f + X * 0.05f) * 3f * Growth;
        g.RotateTransform(sway);

        DrawPlant(g, time, Growth);

        // Penanda siap panen
        if (IsRipe)
        {
            float bob = (float)Math.Sin(time * 4f + X) * 3f;
            using var b = new SolidBrush(Color.FromArgb(230, 255, 240, 120));
            using var p = new Pen(Color.White, 1.5f);
            g.FillEllipse(b, -5, -88 + bob, 10, 10);
            g.DrawEllipse(p, -5, -88 + bob, 10, 10);
        }

        g.Restore(state);
    }

    /// <summary>
    /// Tiap jenis tanaman menggambar dirinya sendiri (POLYMORPHISM).
    /// Override method ini di subclass.
    /// </summary>
    protected abstract void DrawPlant(Graphics g, float time, float growth);
}

// ============================================================
//  IMPLEMENTASI TANAMAN
// ============================================================

/// <summary>Tanaman gandum. Tumbuh dalam 3 hari, jual 15g.</summary>
public class Wheat : Crop
{
    public override string Name => "Gandum";
    public override int GrowthDays => 3;
    public override int SellPrice => 15;

    protected override void DrawPlant(Graphics g, float time, float growth)
    {
        float h = 8 + 44 * growth;
        Color col = Gfx.Lerp(Color.FromArgb(90, 180, 70), Color.Goldenrod, growth);

        for (int i = -1; i <= 1; i++)
        {
            float bx = i * 9;
            float lean = i * 4 * growth;

            using var path = new GraphicsPath();
            path.AddBezier(bx, 0, bx + lean - 4, -h * 0.4f, bx + lean + 4, -h * 0.7f, bx + lean, -h);

            using var pen = new Pen(col, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawPath(pen, path);

            if (growth > 0.5f)
            {
                using var ear = new SolidBrush(col);
                g.FillEllipse(ear, bx + lean - 3, -h - 9 * growth, 6, 12 * growth);
            }
        }
    }
}

/// <summary>Tanaman jagung. Tumbuh dalam 4 hari, jual 25g.</summary>
public class Corn : Crop
{
    public override string Name => "Jagung";
    public override int GrowthDays => 4;
    public override int SellPrice => 25;

    protected override void DrawPlant(Graphics g, float time, float growth)
    {
        float h = 10 + 60 * growth;

        using (var stemPath = new GraphicsPath())
        {
            stemPath.AddBezier(0, 0, -4, -h * 0.4f, 4, -h * 0.7f, 0, -h);
            using var stem = new Pen(Color.FromArgb(80, 160, 60), 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawPath(stem, stemPath);
        }

        using var leaf = new Pen(Color.FromArgb(60, 150, 60), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        for (int k = 1; k <= 3; k++)
        {
            if (growth < k * 0.22f) continue;
            float y = -h * k / 4f;
            float dir = k % 2 == 0 ? 1 : -1;
            float len = 10 + 18 * growth;

            using var lp = new GraphicsPath();
            lp.AddBezier(0, y, dir * len * 0.5f, y - 12, dir * len, y - 8, dir * len * 1.2f, y + 8);
            g.DrawPath(leaf, lp);
        }

        if (IsRipe)
        {
            using var husk = new SolidBrush(Color.FromArgb(90, 170, 70));
            using var cob = new SolidBrush(Color.Gold);
            g.FillEllipse(husk, 2, -h * 0.55f - 2, 11, 22);
            g.FillEllipse(cob, 4, -h * 0.55f, 7, 16);
        }
    }
}

/// <summary>Tanaman tomat. Tumbuh dalam 3 hari, jual 20g.</summary>
public class Tomato : Crop
{
    public override string Name => "Tomat";
    public override int GrowthDays => 3;
    public override int SellPrice => 20;

    protected override void DrawPlant(Graphics g, float time, float growth)
    {
        float r = 6 + 16 * growth;

        using var leaf1 = new SolidBrush(Color.FromArgb(60, 150, 60));
        using var leaf2 = new SolidBrush(Color.FromArgb(80, 175, 75));
        g.FillEllipse(leaf1, -r * 1.4f, -r * 1.6f, r * 2.8f, r * 1.6f);
        g.FillEllipse(leaf2, -r * 0.9f, -r * 2.2f, r * 1.8f, r * 1.4f);

        if (growth > 0.6f)
        {
            float t = (growth - 0.6f) / 0.4f;
            Color fruit = Gfx.Lerp(Color.FromArgb(120, 180, 70), Color.FromArgb(220, 40, 40), t);
            var spots = new[] { (-r * 0.7f, -r * 0.6f), (r * 0.6f, -r * 0.8f), (0f, -r * 0.2f) };

            using var fb = new SolidBrush(fruit);
            using var hl = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
            foreach (var (fx, fy) in spots)
            {
                g.FillEllipse(fb, fx - 5, fy - 5, 10, 10);
                g.FillEllipse(hl, fx - 3, fy - 3, 3, 3); // kilau
            }
        }
    }
}
