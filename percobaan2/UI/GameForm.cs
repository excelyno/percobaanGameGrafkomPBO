using System.Diagnostics;
using System.Drawing.Drawing2D;
using FarmGame.Core;
using FarmGame.Entities;
using FarmGame.Systems;
using FarmGame.Tools;
using Timer = System.Windows.Forms.Timer;

namespace FarmGame.UI;

// ============================================================
//  FORM UTAMA: Rendering + Input + Game Loop
// ============================================================

/// <summary>
/// Form utama game. Bertanggung jawab atas:
/// - Game loop (Timer tick → Update → Invalidate → OnPaint)
/// - Routing input keyboard dan mouse
/// - Rendering semua layer (sky, farm, HUD, minigame)
/// - Implementasi IGameContext (jembatan antara Tools dan state game)
/// </summary>
public class GameForm : Form, IGameContext
{
    // ---- Konstanta layout ----
    private const int W = 960, H = 700;
    private const int Cols = 8, Rows = 5, TileSize = 72;
    private const int OriginX = (W - Cols * TileSize) / 2, OriginY = 250;

    // ---- Data game ----
    private readonly Tile[,] _grid = new Tile[Cols, Rows];
    private Tool[] _tools = Array.Empty<Tool>();
    private int _toolIdx;
    private int _money = 50;

    // ---- Sistem ----
    private readonly DayNightCycle _clock = new();
    private readonly Rain _rain = new(W, H);
    private HarvestMinigame? _mini;

    // ---- Utilitas ----
    private readonly Random _rng = new();
    private readonly Timer _timer = new() { Interval = 16 };
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private float _time;

    // ---- UI state ----
    private Point _mouse;
    private string _toast = "";
    private float _toastT;

    private readonly Font _hud = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _small = new("Segoe UI", 9f, FontStyle.Bold);

    // ---- Dekorasi awan ----
    private class Cloud { public float X, Y, Scale, Speed; }
    private readonly List<Cloud> _clouds = new();

    // ===========================================================
    //  CONSTRUCTOR
    // ===========================================================
    public GameForm()
    {
        Text = "Agro Farm - Prototype (WinForms + GDI+)";
        ClientSize = new Size(W, H);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        InitGrid();
        InitTools();
        InitClouds();

        _clock.NewDay += OnNewDay;
        _timer.Tick += (_, _) => GameTick();
        _timer.Start();
    }

    // ===========================================================
    //  INISIALISASI
    // ===========================================================
    private void InitGrid()
    {
        for (int c = 0; c < Cols; c++)
            for (int r = 0; r < Rows; r++)
                _grid[c, r] = new Tile(new Rectangle(OriginX + c * TileSize, OriginY + r * TileSize, TileSize, TileSize));
    }

    private void InitTools()
    {
        _tools = new Tool[]
        {
            new HoeTool(),
            new WateringCan(),
            new SeedBag("Gandum", 5, () => new Wheat()),
            new SeedBag("Jagung", 8, () => new Corn()),
            new SeedBag("Tomat",  6, () => new Tomato()),
            new Sickle()
        };
    }

    private void InitClouds()
    {
        for (int i = 0; i < 5; i++)
            _clouds.Add(new Cloud
            {
                X     = _rng.Next(W),
                Y     = 30 + _rng.Next(90),
                Scale = 0.7f + (float)_rng.NextDouble() * 0.8f,
                Speed = 8 + _rng.Next(14)
            });
    }

    // ===========================================================
    //  IGameContext
    // ===========================================================
    public void Toast(string message) { _toast = message; _toastT = 2f; }
    public void BeginHarvest(Tile tile) => _mini = new HarvestMinigame(tile);
    public bool TrySpend(int amount)
    {
        if (_money < amount) return false;
        _money -= amount;
        return true;
    }

    // ===========================================================
    //  GAME LOOP
    // ===========================================================
    private void GameTick()
    {
        float dt = Math.Min(0.05f, (float)_sw.Elapsed.TotalSeconds);
        _sw.Restart();
        Update(dt);
        Invalidate();
    }

    private void Update(float dt)
    {
        _time += dt;
        if (_toastT > 0) _toastT -= dt;

        foreach (var c in _clouds)
        {
            c.X += c.Speed * dt;
            if (c.X > W + 120) c.X = -150;
        }

        if (_mini != null)
        {
            _mini.Update(dt);
            if (_mini.Done) FinishHarvest();
            return; // waktu berhenti saat minigame aktif
        }

        _clock.Update(dt);
        _rain.Update(dt);

        // Hujan otomatis menyiram tile yang sudah dicangkul
        if (_rain.Active)
            foreach (var t in _grid)
                if (t.Soil == SoilState.Tilled) t.Soil = SoilState.Watered;
    }

    private void OnNewDay()
    {
        foreach (var t in _grid)
        {
            t.Crop?.OnDayPassed(t.Soil == SoilState.Watered);
            if (t.Soil == SoilState.Watered) t.Soil = SoilState.Tilled;
        }
        _rain.Active = _rng.NextDouble() < 0.25;
        Toast(_rain.Active
            ? $"Hari {_clock.Day}: hujan hari ini!"
            : $"Hari {_clock.Day} dimulai.");
    }

    private void FinishHarvest()
    {
        var m = _mini!;
        var crop = m.Target.RemoveCrop();
        m.Target.Soil = SoilState.Tilled;
        if (crop != null)
        {
            int gain = (int)(crop.SellPrice * m.Multiplier);
            _money += gain;
            Toast($"{crop.Name} terjual +{gain}g  ({m.ResultText})");
        }
        _mini = null;
    }

    // ===========================================================
    //  INPUT
    // ===========================================================
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_mini != null)
        {
            if (e.KeyCode == Keys.Space) _mini.Stop();
            return;
        }

        if (e.KeyCode >= Keys.D1 && e.KeyCode < Keys.D1 + _tools.Length)
            _toolIdx = e.KeyCode - Keys.D1;
        else if (e.KeyCode == Keys.R) _rain.Active = !_rain.Active;
        else if (e.KeyCode == Keys.N) _clock.SkipToNextDay();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mouse = e.Location;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_mini != null || e.Button != MouseButtons.Left) return;

        // Klik toolbar
        for (int i = 0; i < _tools.Length; i++)
            if (ToolSlot(i).Contains(e.Location)) { _toolIdx = i; return; }

        var tile = TileAt(e.Location);
        if (tile != null) _tools[_toolIdx].Use(tile, this);
    }

    // ===========================================================
    //  HELPER LAYOUT
    // ===========================================================
    private Tile? TileAt(Point p)
    {
        int c = (p.X - OriginX) / TileSize, r = (p.Y - OriginY) / TileSize;
        if (p.X < OriginX || p.Y < OriginY || c >= Cols || r >= Rows) return null;
        return _grid[c, r];
    }

    private Rectangle ToolSlot(int i)
    {
        const int slotW = 130, gap = 8;
        int total = _tools.Length * slotW + (_tools.Length - 1) * gap;
        return new Rectangle((W - total) / 2 + i * (slotW + gap), 636, slotW, 50);
    }

    // ===========================================================
    //  RENDER — layer-by-layer
    // ===========================================================
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float d = _clock.Daylight;

        DrawSky(g, d);
        DrawCelestial(g, d);
        DrawClouds(g, d);
        DrawWindmill(g, 110, 130, d);
        DrawHills(g, d);
        DrawFarm(g);
        _rain.Draw(g);

        // Overlay malam: alpha blending
        using (var night = new SolidBrush(Color.FromArgb((int)((1f - d) * 150), 8, 12, 60)))
            g.FillRectangle(night, 0, 0, W, H);

        DrawHud(g);

        if (_mini != null)
        {
            using var dim = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            g.FillRectangle(dim, 0, 0, W, H);
            _mini.Draw(g, new Rectangle(W / 2 - 250, H / 2 - 80, 500, 140));
        }
    }

    // ---- Layer: Langit ----
    private void DrawSky(Graphics g, float d)
    {
        Color top = Gfx.Lerp(Color.FromArgb(8, 10, 35), Color.FromArgb(70, 150, 255), d);
        Color bot = Gfx.Lerp(Color.FromArgb(30, 35, 80), Color.FromArgb(190, 230, 255), d);

        // Warna jingga saat fajar/senja
        float warm = (d > 0f && d < 1f) ? 1f - Math.Abs(2f * d - 1f) : 0f;
        bot = Gfx.Lerp(bot, Color.FromArgb(255, 150, 90), warm * 0.6f);

        using var br = new LinearGradientBrush(new Rectangle(0, 0, W, 270), top, bot, 90f);
        g.FillRectangle(br, 0, 0, W, 270);
    }

    // ---- Layer: Matahari / Bulan ----
    private void DrawCelestial(Graphics g, float d)
    {
        float t = _clock.ArcT;
        float x = 80 + t * (W - 160);
        float y = 200 - (float)Math.Sin(t * Math.PI) * 150;

        if (_clock.IsSunUp)
        {
            Gfx.Glow(g, x, y, 80, Color.FromArgb(170, 255, 230, 130), Color.FromArgb(0, 255, 230, 130));
            using var sun = new SolidBrush(Color.FromArgb(255, 245, 160));
            g.FillEllipse(sun, x - 28, y - 28, 56, 56);
        }
        else
        {
            Gfx.Glow(g, x, y, 60, Color.FromArgb(120, 200, 215, 255), Color.FromArgb(0, 200, 215, 255));
            using var moon = new SolidBrush(Color.FromArgb(235, 240, 255));
            g.FillEllipse(moon, x - 20, y - 20, 40, 40);
            using var crater = new SolidBrush(Color.FromArgb(40, 120, 130, 170));
            g.FillEllipse(crater, x - 8, y - 8, 12, 12);
            g.FillEllipse(crater, x + 4, y + 2, 8, 8);
        }
    }

    // ---- Layer: Awan ----
    private void DrawClouds(Graphics g, float d)
    {
        Color col = Gfx.Lerp(Color.FromArgb(190, 70, 75, 110), Color.FromArgb(200, 255, 255, 255), d);
        if (_rain.Active) col = Gfx.Lerp(col, Color.FromArgb(220, 110, 115, 125), 0.7f);

        using var br = new SolidBrush(col);
        foreach (var c in _clouds)
        {
            var s = g.Save();
            g.TranslateTransform(c.X, c.Y);
            g.ScaleTransform(c.Scale, c.Scale);
            g.FillEllipse(br, 0, 12, 70, 28);
            g.FillEllipse(br, 14, 0, 40, 32);
            g.FillEllipse(br, 38, 8, 46, 30);
            g.Restore(s);
        }
    }

    // ---- Layer: Kincir angin ----
    private void DrawWindmill(Graphics g, float x, float y, float d)
    {
        Color wall = Gfx.Lerp(Color.FromArgb(120, 85, 70), Color.FromArgb(205, 165, 125), d);
        using var wallBr = new SolidBrush(wall);
        using var roofBr = new SolidBrush(Gfx.Lerp(Color.FromArgb(70, 30, 30), Color.FromArgb(170, 60, 50), d));

        g.FillPolygon(wallBr, new[]
        {
            new PointF(x - 22, y + 100), new PointF(x + 22, y + 100),
            new PointF(x + 12, y),       new PointF(x - 12, y)
        });
        g.FillPolygon(roofBr, new[]
        {
            new PointF(x - 16, y + 2), new PointF(x + 16, y + 2), new PointF(x, y - 20)
        });

        // Baling-baling: rotasi dengan matrix transform
        var s = g.Save();
        g.TranslateTransform(x, y);
        g.RotateTransform(_time * 40f);
        using var blade = new SolidBrush(Gfx.Lerp(Color.FromArgb(90, 75, 60), Color.FromArgb(235, 225, 205), d));
        for (int i = 0; i < 4; i++)
        {
            g.FillRectangle(blade, -5, -58, 10, 50);
            g.RotateTransform(90);
        }
        g.Restore(s);

        using var hub = new SolidBrush(Color.FromArgb(60, 45, 40));
        g.FillEllipse(hub, x - 6, y - 6, 12, 12);
    }

    // ---- Layer: Bukit ----
    private void DrawHills(Graphics g, float d)
    {
        DrawHillLayer(g, new PointF[]
        {
            new(0, 220), new(160, 170), new(340, 205), new(540, 160), new(760, 200), new(960, 175)
        }, Gfx.Lerp(Color.FromArgb(25, 45, 40), Color.FromArgb(110, 170, 120), d));

        DrawHillLayer(g, new PointF[]
        {
            new(0, 250), new(200, 215), new(420, 245), new(660, 210), new(960, 240)
        }, Gfx.Lerp(Color.FromArgb(25, 60, 40), Color.FromArgb(80, 150, 85), d));

        using var ground = new LinearGradientBrush(new Rectangle(0, 240, W, H - 240),
            Gfx.Lerp(Color.FromArgb(30, 70, 45),  Color.FromArgb(100, 175, 75), d),
            Gfx.Lerp(Color.FromArgb(20, 50, 35),  Color.FromArgb(60, 130, 55),  d), 90f);
        g.FillRectangle(ground, 0, 240, W, H - 240);
    }

    private static void DrawHillLayer(Graphics g, PointF[] pts, Color color)
    {
        using var path = new GraphicsPath();
        path.AddCurve(pts, 0.5f); // spline halus
        path.AddLine(W, 330, W, 330);
        path.AddLine(W, 330, 0, 330);
        path.CloseFigure();
        using var br = new SolidBrush(color);
        g.FillPath(br, path);
    }

    // ---- Layer: Lahan & Tanaman ----
    private void DrawFarm(Graphics g)
    {
        var hover = TileAt(_mouse);

        // Gambar tile tanah
        foreach (var t in _grid)
        {
            Rectangle b = t.Bounds;
            Color baseCol = t.Soil switch
            {
                SoilState.Tilled  => Color.FromArgb(125, 85, 48),
                SoilState.Watered => Color.FromArgb(78, 52, 32),
                _ => ((b.X / TileSize + b.Y / TileSize) % 2 == 0)
                    ? Color.FromArgb(96, 170, 72)
                    : Color.FromArgb(88, 160, 66)
            };

            using var fill = new SolidBrush(baseCol);
            g.FillRectangle(fill, b);

            if (t.Soil != SoilState.Grass)
            {
                using var furrow = new Pen(Color.FromArgb(70, 0, 0, 0), 2f);
                for (int i = 1; i <= 3; i++)
                    g.DrawLine(furrow, b.X + 6, b.Y + i * 18, b.Right - 6, b.Y + i * 18);
            }

            using var edge = new Pen(Color.FromArgb(40, 0, 0, 0));
            g.DrawRectangle(edge, b);
        }

        // Gambar tanaman per baris (painter's algorithm / depth sort)
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                _grid[c, r].Crop?.Draw(g, _time); // POLYMORPHISM

        // Highlight tile di bawah mouse
        if (hover != null && _mini == null)
        {
            using var hb = new SolidBrush(Color.FromArgb(55, 255, 255, 255));
            using var hp = new Pen(Color.FromArgb(200, 255, 255, 255), 2f);
            g.FillRectangle(hb, hover.Bounds);
            g.DrawRectangle(hp, hover.Bounds);
        }
    }

    // ---- Layer: HUD ----
    private void DrawHud(Graphics g)
    {
        // Panel info kiri atas
        int hh = (int)_clock.Hour;
        int mm = (int)((_clock.Hour - hh) * 60) / 10 * 10;
        string info = $"Hari {_clock.Day}   {hh:00}:{mm:00}   Uang: {_money}g" +
                      (_rain.Active ? "   [Hujan]" : "");

        var panel = new RectangleF(12, 12, 340, 34);
        using (var p = Gfx.RoundRect(panel, 10))
        using (var f = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            g.FillPath(f, p);
        g.DrawString(info, _hud, Brushes.White, 24, 18);

        // Bantuan keyboard kanan atas
        using var sfR = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString("[1-6] pilih alat   [R] hujan   [N] skip hari   Klik tile untuk pakai alat",
            _small, Brushes.White, new RectangleF(0, 20, W - 14, 20), sfR);

        // Toolbar bawah
        for (int i = 0; i < _tools.Length; i++)
        {
            var slot = ToolSlot(i);
            bool sel = i == _toolIdx;
            using var path   = Gfx.RoundRect(slot, 10);
            using var bg     = new SolidBrush(Color.FromArgb(sel ? 230 : 170, sel ? 90 : 20, sel ? 70 : 20, sel ? 20 : 20));
            using var border = new Pen(sel ? Color.Gold : Color.FromArgb(120, 255, 255, 255), sel ? 3f : 1.5f);
            g.FillPath(bg, path);
            g.DrawPath(border, path);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString($"{i + 1}\n{_tools[i].Label}", _small, Brushes.White, slot, sf);
        }

        // Toast notification
        if (_toastT > 0)
        {
            int a = (int)(Math.Min(1f, _toastT) * 230);
            using var sf     = new StringFormat { Alignment = StringAlignment.Center };
            using var tb     = new SolidBrush(Color.FromArgb(a, 255, 255, 255));
            using var shadow = new SolidBrush(Color.FromArgb(a, 0, 0, 0));
            g.DrawString(_toast, _hud, shadow, new RectangleF(1, 61, W, 30), sf);
            g.DrawString(_toast, _hud, tb,     new RectangleF(0, 60, W, 30), sf);
        }
    }

    // ===========================================================
    //  DISPOSE
    // ===========================================================
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _hud.Dispose();
            _small.Dispose();
        }
        base.Dispose(disposing);
    }
}
