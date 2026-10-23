using System.Drawing;
using System.Drawing.Drawing2D;
using FarmGame.Core;

namespace FarmGame.World;

// ============================================================
//  BUILD MODE — tempatkan / hapus tile dengan mouse
// ============================================================

/// <summary>
/// Mode bangun: pemain bisa klik tile di peta untuk
/// menggantinya, seperti editor level sederhana.
/// Toggle dengan tombol B.
/// </summary>
public class BuildMode
{
    public bool Active { get; private set; }

    // Tile yang sedang dipilih untuk ditaruh
    private TileType _selectedTile = TileType.Path;
    private readonly TileType[] _palette =
    [
        TileType.Grass,
        TileType.Path,
        TileType.Stone,
        TileType.Water,
        TileType.Tree,
        TileType.Flower,
        TileType.Farm,
        TileType.House,
    ];
    private int _paletteIdx = 1;

    private Point _hoveredCell = new(-1, -1);

    // ============================================================
    public void Toggle() => Active = !Active;

    public void Update(InputState input, TileMap map, Camera cam)
    {
        if (!Active) return;

        // Hover: konversi mouse screen → world → cell
        var world = cam.ScreenToWorld(input.MousePos);
        int col = (int)(world.X / TileMap.TileSize);
        int row = (int)(world.Y / TileMap.TileSize);
        _hoveredCell = new Point(col, row);

        // Klik kiri → taruh tile
        if (input.MouseLeft)
            map.SetTile(col, row, _selectedTile);

        // Klik kanan → hapus → Grass
        if (input.MouseRight)
            map.SetTile(col, row, TileType.Grass);

        // Scroll palette dengan Q / E
        // (bisa juga dari mouse wheel — lihat GameForm)
    }

    /// <summary>
    /// Ganti tile yang dipilih (dipanggil dari scroll wheel).
    /// </summary>
    public void CyclePalette(int delta)
    {
        _paletteIdx = ((_paletteIdx + delta) % _palette.Length + _palette.Length) % _palette.Length;
        _selectedTile = _palette[_paletteIdx];
    }

    // ============================================================
    //  RENDER OVERLAY
    // ============================================================

    public void Draw(Graphics g, Camera cam)
    {
        if (!Active) return;

        // Highlight cell di bawah mouse
        if (_hoveredCell.X >= 0)
        {
            float wx = _hoveredCell.X * TileMap.TileSize;
            float wy = _hoveredCell.Y * TileMap.TileSize;
            using var hb = new SolidBrush(Color.FromArgb(80, 255, 255, 100));
            using var hp = new Pen(Color.Yellow, 2f);
            g.FillRectangle(hb, wx, wy, TileMap.TileSize, TileMap.TileSize);
            g.DrawRectangle(hp, wx, wy, TileMap.TileSize, TileMap.TileSize);
        }
    }

    /// <summary>
    /// Render panel HUD di atas — tanpa transformasi kamera.
    /// </summary>
    public void DrawHud(Graphics g, int screenW)
    {
        if (!Active) return;

        // Banner atas
        using (var bg = new SolidBrush(Color.FromArgb(200, 20, 20, 40)))
            g.FillRectangle(bg, 0, 0, screenW, 50);

        using var font  = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var small = new Font("Segoe UI", 9f);
        using var sfC   = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        g.DrawString("⚒ BUILD MODE — Klik Kiri: taruh  |  Klik Kanan: hapus  |  Scroll: ganti tile  |  B: keluar",
            small, Brushes.Yellow, new RectangleF(0, 0, screenW, 50), sfC);

        // Palette horizontal di bawah banner
        int slotSize = 48;
        int total    = _palette.Length * slotSize;
        int startX   = (screenW - total) / 2;
        int py       = 54;

        for (int i = 0; i < _palette.Length; i++)
        {
            var tileType = _palette[i];
            var rect = new Rectangle(startX + i * slotSize, py, slotSize - 2, slotSize - 2);
            bool sel = i == _paletteIdx;

            // Background
            using var bg = new SolidBrush(Color.FromArgb(sel ? 220 : 160, 30, 30, 50));
            g.FillRectangle(bg, rect);

            // Border
            using var border = new Pen(sel ? Color.Yellow : Color.FromArgb(100, 200, 200, 200), sel ? 2.5f : 1f);
            g.DrawRectangle(border, rect);

            // Label singkat
            string label = tileType.ToString()[..Math.Min(4, tileType.ToString().Length)];
            using var lf = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var lb = new SolidBrush(sel ? Color.Yellow : Color.White);
            g.DrawString(label, lf, lb, new RectangleF(rect.X, rect.Y + 14, rect.Width, 20), sfC);
        }
    }
}
