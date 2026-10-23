using System.Drawing;
using System.Drawing.Drawing2D;

namespace FarmGame;

// ============================================================
//  HELPER GRAFIKA (konsep ala Cairo: path, gradient, alpha)
// ============================================================
public static class Gfx
{
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

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

    // Cahaya lembut: radial gradient dari tengah (opaque) ke tepi (transparan)
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

// ============================================================
//  OOP: INTERFACE + BASE CLASS
// ============================================================
public interface IRenderable
{
    void Draw(Graphics g, float time);
}

public interface IGameContext
{
    void Toast(string message);
    void BeginHarvest(Tile tile);
    bool TrySpend(int amount);
}

public abstract class GameObject
{
    // Encapsulation: posisi hanya bisa diubah lewat method
    public float X { get; private set; }
    public float Y { get; private set; }

    public void SetPosition(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// ============================================================
//  TANAMAN: Abstraction + Inheritance + Polymorphism
// ============================================================
public abstract class Crop : GameObject, IRenderable
{
    private int _daysGrown; // Encapsulation: tidak bisa diubah dari luar

    public abstract string Name { get; }
    public abstract int GrowthDays { get; }
    public abstract int SellPrice { get; }

    public float Growth => Math.Min(1f, (float)_daysGrown / GrowthDays);
    public bool IsRipe => _daysGrown >= GrowthDays;

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

    // Tiap jenis tanaman menggambar dirinya sendiri (POLYMORPHISM)
    protected abstract void DrawPlant(Graphics g, float time, float growth);
}

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

// ============================================================
//  TILE / LAHAN
// ============================================================
public enum SoilState { Grass, Tilled, Watered }

public class Tile
{
    public Rectangle Bounds { get; }
    public SoilState Soil { get; set; } = SoilState.Grass;
    public Crop? Crop { get; private set; }

    public Tile(Rectangle bounds) => Bounds = bounds;

    public bool Plant(Crop crop)
    {
        if (Soil == SoilState.Grass || Crop != null) return false;
        crop.SetPosition(Bounds.X + Bounds.Width / 2f, Bounds.Bottom - 10);
        Crop = crop;
        return true;
    }

    public Crop? RemoveCrop()
    {
        var c = Crop;
        Crop = null;
        return c;
    }
}

// ============================================================
//  ALAT: Abstraction + Polymorphism (Strategy pattern)
// ============================================================
public abstract class Tool
{
    public abstract string Label { get; }
    public abstract void Use(Tile tile, IGameContext ctx);
}

public class HoeTool : Tool
{
    public override string Label => "Cangkul";

    public override void Use(Tile tile, IGameContext ctx)
    {
        if (tile.Soil == SoilState.Grass) tile.Soil = SoilState.Tilled;
        else ctx.Toast("Tanah ini sudah dicangkul.");
    }
}

public class WateringCan : Tool
{
    public override string Label => "Penyiram";

    public override void Use(Tile tile, IGameContext ctx)
    {
        if (tile.Soil == SoilState.Tilled) tile.Soil = SoilState.Watered;
        else if (tile.Soil == SoilState.Grass) ctx.Toast("Cangkul dulu tanahnya.");
    }
}

public class SeedBag : Tool
{
    private readonly string _name;
    private readonly int _cost;
    private readonly Func<Crop> _factory; // Factory sederhana

    public SeedBag(string name, int cost, Func<Crop> factory)
    {
        _name = name;
        _cost = cost;
        _factory = factory;
    }

    public override string Label => $"Bibit {_name} ({_cost}g)";

    public override void Use(Tile tile, IGameContext ctx)
    {
        if (tile.Soil == SoilState.Grass) { ctx.Toast("Cangkul tanahnya dulu."); return; }
        if (tile.Crop != null) { ctx.Toast("Sudah ada tanaman."); return; }
        if (!ctx.TrySpend(_cost)) { ctx.Toast("Uang tidak cukup!"); return; }
        tile.Plant(_factory());
    }
}

public class Sickle : Tool
{
    public override string Label => "Sabit (panen)";

    public override void Use(Tile tile, IGameContext ctx)
    {
        if (tile.Crop == null) { ctx.Toast("Tidak ada tanaman."); return; }
        if (!tile.Crop.IsRipe) { ctx.Toast($"{tile.Crop.Name} belum matang."); return; }
        ctx.BeginHarvest(tile); // memicu minigame
    }
}

// ============================================================
//  SIKLUS WAKTU
// ============================================================
public class DayNightCycle
{
    public const float DayLengthSeconds = 24f; // 1 hari game = 24 detik nyata

    public int Day { get; private set; } = 1;
    public float Hour { get; private set; } = 6f;
    public event Action? NewDay;

    public void Update(float dt)
    {
        Hour += dt / DayLengthSeconds * 24f;
        if (Hour >= 24f)
        {
            Hour -= 24f;
            Day++;
            NewDay?.Invoke();
        }
    }

    public void SkipToNextDay() => Hour = 23.99f;

    // 0 = malam, 1 = siang. Transisi halus saat fajar dan senja.
    public float Daylight
    {
        get
        {
            if (Hour < 5f || Hour > 19f) return 0f;
            if (Hour < 7f) return (Hour - 5f) / 2f;
            if (Hour > 17f) return (19f - Hour) / 2f;
            return 1f;
        }
    }

    // Posisi 0..1 sepanjang busur langit untuk matahari (06-18) atau bulan (18-06)
    public bool IsSunUp => Hour >= 6f && Hour < 18f;
    public float ArcT => IsSunUp ? (Hour - 6f) / 12f : (Hour >= 18f ? Hour - 18f : Hour + 6f) / 12f;
}

// ============================================================
//  HUJAN (partikel + alpha)
// ============================================================
public class Rain
{
    private class Drop { public float X, Y, Speed; }

    private readonly List<Drop> _drops = new();
    private readonly Random _rng = new();
    private readonly int _w, _h;

    public bool Active { get; set; }

    public Rain(int w, int h)
    {
        _w = w;
        _h = h;
        for (int i = 0; i < 220; i++)
            _drops.Add(new Drop { X = _rng.Next(w + 100), Y = _rng.Next(h), Speed = 500 + _rng.Next(300) });
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

// ============================================================
//  MINIGAME PANEN: tekan SPACE saat kursor di zona hijau
// ============================================================
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

    public void Stop()
    {
        if (Finished) return;
        float d = Math.Abs(_cursor - _center);

        if (d < _width * 0.2f) { Multiplier = 2f; ResultText = "PERFECT!  x2"; }
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