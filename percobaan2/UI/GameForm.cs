using System.Diagnostics;
using System.Drawing.Drawing2D;
using FarmGame.Core;
using FarmGame.Entities;
using FarmGame.Systems;
using FarmGame.Tools;
using FarmGame.World;
using Timer = System.Windows.Forms.Timer;

namespace FarmGame.UI;

// ============================================================
//  FORM UTAMA: Game Loop + Input + Render
// ============================================================

/// <summary>
/// Form utama game. Menggabungkan:
/// - World map (TileMap dari file .map)
/// - Player (vector character, WASD + Gamepad)
/// - Camera (smooth follow, world-space rendering)
/// - Build Mode (edit tile dengan mouse)
/// - Farm grid (lahan tanam + minigame panen)
/// - Day/Night cycle, Rain system, HUD
/// </summary>
public class GameForm : Form, IGameContext
{
    // ---- Konstanta layar ----
    private const int W = 1024, H = 720;

    // ---- Farm grid (overlay di atas TileMap farm zone) ----
    private const int FarmCols = 6, FarmRows = 4;
    private const int FarmTileSize = 48; // sama dengan TileMap.TileSize

    // ---- Input ----
    private readonly InputState     _input  = new();
    private readonly HashSet<Keys>  _held   = new();
    private bool _mouseLBtn, _mouseRBtn;

    // ---- World ----
    private TileMap   _map    = null!;
    private BuildMode _build  = new();
    private readonly Player _player = new();
    private readonly Camera _camera = null!; // init di ctor

    // ---- Farm ----
    private Tile[,]? _farmGrid;
    private Tool[]   _tools = Array.Empty<Tool>();
    private int      _toolIdx;
    private int      _money = 50;

    // ---- Sistem ----
    private readonly DayNightCycle _clock = new();
    private readonly Rain          _rain;
    private HarvestMinigame?       _mini;

    // ---- Loop ----
    private readonly Timer     _timer = new() { Interval = 16 };
    private readonly Stopwatch _sw    = Stopwatch.StartNew();
    private float _time;

    // ---- HUD ----
    private string _toast = "";
    private float  _toastT;
    private readonly Font _hud   = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _small = new("Segoe UI",  9f, FontStyle.Bold);

    // ---- Awan ----
    private class Cloud { public float X, Y, Scale, Speed; }
    private readonly List<Cloud> _clouds = new();
    private readonly Random      _rng    = new();

    // ===========================================================
    //  CONSTRUCTOR
    // ===========================================================
    public GameForm()
    {
        Text = "Agro Farm — Vector World";
        ClientSize = new Size(W, H);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        // Load peta
        LoadMap();

        // Kamera: dunia seluas TileMap
        _camera = new Camera(W, H, _map.PixelW, _map.PixelH);

        // Posisi awal player di tengah peta
        _player.SetPosition(_map.PixelW / 2f, _map.PixelH / 2f);
        _camera.SnapTo(_player.X, _player.Y);

        // Rain seukuran viewport (partikel screen-space)
        _rain = new Rain(W, H);

        InitFarmGrid();
        InitTools();
        InitClouds();

        _clock.NewDay += OnNewDay;
        _timer.Tick   += (_, _) => GameTick();
        _timer.Start();
    }

    // ===========================================================
    //  INISIALISASI
    // ===========================================================
    private void LoadMap()
    {
        string mapPath = Path.Combine(AppContext.BaseDirectory, "World", "default.map");
        _map = File.Exists(mapPath)
            ? TileMap.FromFile(mapPath)
            : TileMap.FromLines(TileMap.DefaultMapLines);
    }

    private void InitFarmGrid()
    {
        // Cari zona Farm (tile 'X') dari peta dan buat Tile-grid di atasnya
        // Untuk kesederhanaan: buat grid fixed 6×4 yang di-overlay
        _farmGrid = new Tile[FarmCols, FarmRows];
        // Cari titik kiri-atas zona Farm di peta
        int farmOriginC = 0, farmOriginR = 0;
        for (int r = 0; r < _map.Rows && farmOriginC == 0; r++)
            for (int c = 0; c < _map.Cols && farmOriginC == 0; c++)
                if (_map.CellAt(c, r)?.Type == TileType.Farm)
                { farmOriginC = c; farmOriginR = r; }

        for (int c = 0; c < FarmCols; c++)
            for (int r = 0; r < FarmRows; r++)
            {
                int wx = (farmOriginC + c) * FarmTileSize;
                int wy = (farmOriginR + r) * FarmTileSize;
                _farmGrid[c, r] = new Tile(new Rectangle(wx, wy, FarmTileSize, FarmTileSize));
            }
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
        for (int i = 0; i < 6; i++)
            _clouds.Add(new Cloud
            {
                X     = _rng.Next(W),
                Y     = 20 + _rng.Next(80),
                Scale = 0.7f + (float)_rng.NextDouble() * 0.8f,
                Speed = 8 + _rng.Next(14)
            });
    }

    // ===========================================================
    //  IGameContext
    // ===========================================================
    public void Toast(string msg) { _toast = msg; _toastT = 2.5f; }
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

        // Poll input (keyboard + mouse — gamepad bisa ditambah)
        _input.Poll(_held, PointToClient(Cursor.Position), _mouseLBtn, _mouseRBtn);

        Update(dt);
        Invalidate();
    }

    private void Update(float dt)
    {
        _time += dt;
        if (_toastT > 0) _toastT -= dt;

        // Awan (screen-space)
        foreach (var c in _clouds)
        {
            c.X += c.Speed * dt;
            if (c.X > W + 120) c.X = -150;
        }

        // Toggle build mode
        if (_input.JustBuild) _build.Toggle();

        // Build mode update (mouse pada dunia)
        if (_build.Active)
        {
            _build.Update(_input, _map, _camera);
            return; // freeze time saat build
        }

        // Minigame panen
        if (_mini != null)
        {
            _mini.Update(dt);
            if (_mini.Done) FinishHarvest();
            return;
        }

        // ---- Update normal ----
        _player.Update(_input, dt);
        _player.ClampToWorld(_map.PixelW, _map.PixelH);
        _camera.Follow(_player.X, _player.Y, dt);

        _clock.Update(dt);
        _rain.Update(dt);

        if (_rain.Active && _farmGrid != null)
            foreach (var t in _farmGrid)
                if (t.Soil == SoilState.Tilled) t.Soil = SoilState.Watered;
    }

    private void OnNewDay()
    {
        if (_farmGrid != null)
            foreach (var t in _farmGrid)
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
        var m    = _mini!;
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
        _held.Add(e.KeyCode);

        if (_mini != null)
        {
            if (e.KeyCode == Keys.Space) _mini.Stop();
            return;
        }

        if (!_build.Active)
        {
            if (e.KeyCode >= Keys.D1 && e.KeyCode < Keys.D1 + _tools.Length)
                _toolIdx = e.KeyCode - Keys.D1;
            else if (e.KeyCode == Keys.R) _rain.Active = !_rain.Active;
            else if (e.KeyCode == Keys.N) _clock.SkipToNextDay();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _held.Remove(e.KeyCode);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)  _mouseLBtn = true;
        if (e.Button == MouseButtons.Right) _mouseRBtn = true;

        // Klik farm tile (hanya jika tidak build mode)
        if (!_build.Active && _mini == null && e.Button == MouseButtons.Left)
        {
            // Cek toolbar dulu
            for (int i = 0; i < _tools.Length; i++)
                if (ToolSlot(i).Contains(e.Location)) { _toolIdx = i; return; }

            // Klik dunia → konversi ke farm tile
            var world = _camera.ScreenToWorld(e.Location);
            var tile = FarmTileAt(world);
            if (tile != null) _tools[_toolIdx].Use(tile, this);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)  _mouseLBtn = false;
        if (e.Button == MouseButtons.Right) _mouseRBtn = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_build.Active)
            _build.CyclePalette(e.Delta > 0 ? -1 : 1);
        else
        {
            // Zoom kamera dengan scroll
            _camera.Zoom = Math.Clamp(_camera.Zoom + (e.Delta > 0 ? 0.1f : -0.1f), 0.5f, 2.5f);
        }
    }

    private Tile? FarmTileAt(System.Drawing.PointF world)
    {
        if (_farmGrid == null) return null;
        for (int c = 0; c < FarmCols; c++)
            for (int r = 0; r < FarmRows; r++)
                if (_farmGrid[c, r].Bounds.Contains((int)world.X, (int)world.Y))
                    return _farmGrid[c, r];
        return null;
    }

    private Rectangle ToolSlot(int i)
    {
        const int slotW = 110, gap = 6;
        int total = _tools.Length * slotW + (_tools.Length - 1) * gap;
        return new Rectangle((W - total) / 2 + i * (slotW + gap), H - 60, slotW, 48);
    }

    // ===========================================================
    //  RENDER
    // ===========================================================
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float d = _clock.Daylight;

        // --- Layer 1: Langit (screen-space) ---
        DrawSky(g, d);
        DrawCelestial(g, d);
        DrawClouds(g, d);

        // --- Layer 2: Dunia (world-space, pakai kamera) ---
        _camera.ApplyTransform(g);
        _map.Draw(g, _camera);

        // Farm overlay
        DrawFarmGrid(g);

        // Build mode highlight (world-space)
        _build.Draw(g, _camera);

        // Player
        _player.Draw(g, _time);

        // --- Layer 3: Rain (screen-space) ---
        g.ResetTransform();
        _rain.Draw(g);

        // --- Layer 4: Night overlay ---
        using (var night = new SolidBrush(Color.FromArgb((int)((1f - d) * 160), 8, 12, 60)))
            g.FillRectangle(night, 0, 0, W, H);

        // --- Layer 5: HUD (screen-space) ---
        _build.DrawHud(g, W);
        DrawHud(g);

        // --- Layer 6: Minigame ---
        if (_mini != null)
        {
            using var dim = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            g.FillRectangle(dim, 0, 0, W, H);
            _mini.Draw(g, new Rectangle(W / 2 - 250, H / 2 - 80, 500, 140));
        }
    }

    // ---- Sky ----
    private void DrawSky(Graphics g, float d)
    {
        Color top = Gfx.Lerp(Color.FromArgb(8, 10, 35), Color.FromArgb(70, 150, 255), d);
        Color bot = Gfx.Lerp(Color.FromArgb(30, 35, 80), Color.FromArgb(190, 230, 255), d);
        float warm = (d > 0f && d < 1f) ? 1f - Math.Abs(2f * d - 1f) : 0f;
        bot = Gfx.Lerp(bot, Color.FromArgb(255, 150, 90), warm * 0.6f);
        using var br = new LinearGradientBrush(new Rectangle(0, 0, W, 200), top, bot, 90f);
        g.FillRectangle(br, 0, 0, W, 200);
    }

    // ---- Celestial ----
    private void DrawCelestial(Graphics g, float d)
    {
        float t = _clock.ArcT;
        float x = 80 + t * (W - 160);
        float y = 160 - (float)Math.Sin(t * Math.PI) * 120;

        if (_clock.IsSunUp)
        {
            Gfx.Glow(g, x, y, 70, Color.FromArgb(170, 255, 230, 130), Color.FromArgb(0, 255, 230, 130));
            using var sun = new SolidBrush(Color.FromArgb(255, 245, 160));
            g.FillEllipse(sun, x - 24, y - 24, 48, 48);
        }
        else
        {
            Gfx.Glow(g, x, y, 50, Color.FromArgb(120, 200, 215, 255), Color.FromArgb(0, 200, 215, 255));
            using var moon = new SolidBrush(Color.FromArgb(235, 240, 255));
            g.FillEllipse(moon, x - 18, y - 18, 36, 36);
            using var crater = new SolidBrush(Color.FromArgb(40, 120, 130, 170));
            g.FillEllipse(crater, x - 6, y - 6, 10, 10);
            g.FillEllipse(crater, x + 4, y + 2, 7, 7);
        }
    }

    // ---- Clouds ----
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

    // ---- Farm Grid overlay ----
    private void DrawFarmGrid(Graphics g)
    {
        if (_farmGrid == null) return;

        foreach (var t in _farmGrid)
        {
            Rectangle b = t.Bounds;
            // Hanya gambar jika tile di peta memang Farm
            var mapCell = _map.CellAtWorld(b.X + 4, b.Y + 4);
            if (mapCell?.Type != TileType.Farm) continue;

            Color baseCol = t.Soil switch
            {
                SoilState.Tilled  => Color.FromArgb(140, 95, 55),
                SoilState.Watered => Color.FromArgb(90, 60, 35),
                _                 => Color.FromArgb(125, 85, 48)
            };
            using var fill = new SolidBrush(baseCol);
            g.FillRectangle(fill, b);

            if (t.Soil != SoilState.Grass)
            {
                using var furrow = new Pen(Color.FromArgb(70, 0, 0, 0), 1.5f);
                for (int i = 1; i <= 2; i++)
                    g.DrawLine(furrow, b.X + 4, b.Y + i * 16, b.Right - 4, b.Y + i * 16);
            }

            using var edge = new Pen(Color.FromArgb(40, 0, 0, 0));
            g.DrawRectangle(edge, b);
        }

        // Tanaman (painter's algorithm — gambar dari baris atas ke bawah)
        for (int r = 0; r < FarmRows; r++)
            for (int c = 0; c < FarmCols; c++)
                _farmGrid[c, r].Crop?.Draw(g, _time);
    }

    // ---- HUD ----
    private void DrawHud(Graphics g)
    {
        if (_build.Active) return; // build mode punya HUD-nya sendiri

        // Info panel kiri atas
        int hh = (int)_clock.Hour;
        int mm = (int)((_clock.Hour - hh) * 60) / 10 * 10;
        string info = $"Hari {_clock.Day}   {hh:00}:{mm:00}   Uang: {_money}g" +
                      (_rain.Active ? "   [Hujan]" : "");

        var panel = new RectangleF(12, 12, 360, 34);
        using (var p = Gfx.RoundRect(panel, 10))
        using (var f = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            g.FillPath(f, p);
        g.DrawString(info, _hud, Brushes.White, 24f, 18f);

        // Panduan
        using var sfR = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString("WASD/↑↓←→: gerak  B: build mode  R: hujan  N: skip  Scroll: zoom",
            _small, Brushes.White, new RectangleF(0, 20, W - 14, 20), sfR);

        // Toolbar farm tools
        for (int i = 0; i < _tools.Length; i++)
        {
            var slot = ToolSlot(i);
            bool sel = i == _toolIdx;
            using var path   = Gfx.RoundRect(slot, 10);
            using var bg     = new SolidBrush(Color.FromArgb(sel ? 230 : 160,
                                                               sel ?  90 : 20,
                                                               sel ?  70 : 20,
                                                               sel ?  20 : 20));
            using var border = new Pen(sel ? Color.Gold : Color.FromArgb(120, 255, 255, 255),
                                       sel ? 3f : 1.5f);
            g.FillPath(bg, path);
            g.DrawPath(border, path);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString($"{i + 1}\n{_tools[i].Label}", _small, Brushes.White, slot, sf);
        }

        // Toast
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
        if (disposing) { _timer.Dispose(); _hud.Dispose(); _small.Dispose(); }
        base.Dispose(disposing);
    }
}
