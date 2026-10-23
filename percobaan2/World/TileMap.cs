using System.Drawing;

namespace FarmGame.World;

// ============================================================
//  TILEMAP — peta dari file teks (.map)
// ============================================================

/// <summary>
/// Jenis tile yang tersedia di peta.
/// Karakter teks di file .map → TileType.
/// </summary>
public enum TileType
{
    Grass   = '.',  // tanah rumput biasa
    Water   = '~',  // air / sungai
    Path    = ',',  // jalan tanah
    Stone   = '#',  // batu / dinding
    Tree    = 'T',  // pohon (solid, blocker)
    Flower  = 'F',  // bunga dekoratif
    Farm    = 'X',  // lahan farm (bisa dicangkul)
    House   = 'H',  // bangunan (blocker)
    Empty   = ' ',  // void / luar peta
}

/// <summary>
/// Satu sel peta.
/// </summary>
public class MapCell
{
    public TileType Type   { get; set; }
    public bool     Solid  { get; }  // apakah menghalangi pergerakan
    public bool     IsFarm { get; }  // apakah bisa dijadikan lahan
    public Color    BaseColor { get; }

    private static readonly Dictionary<TileType, (bool solid, bool farm, Color color)> _meta = new()
    {
        [TileType.Grass]   = (false, false, Color.FromArgb(96, 170, 72)),
        [TileType.Water]   = (true,  false, Color.FromArgb(60, 120, 200)),
        [TileType.Path]    = (false, false, Color.FromArgb(180, 155, 110)),
        [TileType.Stone]   = (true,  false, Color.FromArgb(120, 115, 110)),
        [TileType.Tree]    = (true,  false, Color.FromArgb(40, 110, 50)),
        [TileType.Flower]  = (false, false, Color.FromArgb(90, 160, 70)),
        [TileType.Farm]    = (false, true,  Color.FromArgb(125, 85, 48)),
        [TileType.House]   = (true,  false, Color.FromArgb(180, 140, 100)),
        [TileType.Empty]   = (true,  false, Color.FromArgb(10, 10, 15)),
    };

    public MapCell(TileType type)
    {
        Type = type;
        var (solid, farm, color) = _meta.TryGetValue(type, out var m)
            ? m : (false, false, Color.Gray);
        Solid     = solid;
        IsFarm    = farm;
        BaseColor = color;
    }
}

/// <summary>
/// Peta dunia 2D yang di-load dari file teks.
/// Format file: setiap baris = satu baris tile, setiap karakter = satu tile.
/// </summary>
public class TileMap
{
    public const int TileSize = 48; // piksel per tile

    public int Cols { get; private set; }
    public int Rows { get; private set; }
    public int PixelW => Cols * TileSize;
    public int PixelH => Rows * TileSize;

    private MapCell[,] _cells = new MapCell[0, 0];

    // ============================================================
    //  LOAD
    // ============================================================

    /// <summary>
    /// Load peta dari file .map.
    /// Setiap baris file = satu baris tile; setiap karakter = satu sel.
    /// </summary>
    public static TileMap FromFile(string path)
    {
        var lines = File.ReadAllLines(path);
        return FromLines(lines);
    }

    /// <summary>
    /// Load peta dari array string (berguna untuk embedded default map).
    /// </summary>
    public static TileMap FromLines(string[] lines)
    {
        int rows = lines.Length;
        int cols = lines.Max(l => l.Length);
        var map  = new TileMap { Cols = cols, Rows = rows };
        map._cells = new MapCell[cols, rows];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char ch = c < lines[r].Length ? lines[r][c] : ' ';
                var type = Enum.IsDefined(typeof(TileType), (int)ch)
                    ? (TileType)ch
                    : TileType.Grass;
                map._cells[c, r] = new MapCell(type);
            }
        }

        return map;
    }

    // ============================================================
    //  AKSES SEL
    // ============================================================

    public MapCell? CellAt(int col, int row)
    {
        if (col < 0 || row < 0 || col >= Cols || row >= Rows) return null;
        return _cells[col, row];
    }

    public MapCell? CellAtWorld(float wx, float wy)
        => CellAt((int)(wx / TileSize), (int)(wy / TileSize));

    public bool IsSolid(float wx, float wy)
        => CellAtWorld(wx, wy)?.Solid ?? true;

    /// <summary>Ubah jenis tile (dipakai build mode).</summary>
    public bool SetTile(int col, int row, TileType type)
    {
        if (col < 0 || row < 0 || col >= Cols || row >= Rows) return false;
        _cells[col, row] = new MapCell(type);
        return true;
    }

    // ============================================================
    //  RENDER (hanya tile yang terlihat oleh kamera)
    // ============================================================

    public void Draw(System.Drawing.Graphics g, FarmGame.Core.Camera cam)
    {
        // Culling: hanya gambar tile dalam viewport
        int c0 = Math.Max(0, (int)((cam.X - cam.ViewW / cam.Zoom / 2f) / TileSize) - 1);
        int r0 = Math.Max(0, (int)((cam.Y - cam.ViewH / cam.Zoom / 2f) / TileSize) - 1);
        int c1 = Math.Min(Cols - 1, c0 + (int)(cam.ViewW / cam.Zoom / TileSize) + 3);
        int r1 = Math.Min(Rows - 1, r0 + (int)(cam.ViewH / cam.Zoom / TileSize) + 3);

        for (int r = r0; r <= r1; r++)
        {
            for (int c = c0; c <= c1; c++)
            {
                var cell = _cells[c, r];
                float wx = c * TileSize;
                float wy = r * TileSize;
                var rect = new System.Drawing.RectangleF(wx, wy, TileSize, TileSize);

                // Warna dasar tile
                using var fill = new System.Drawing.SolidBrush(cell.BaseColor);
                g.FillRectangle(fill, rect);

                // Detail visual per tipe
                DrawTileDetail(g, cell.Type, wx, wy);

                // Grid line tipis
                using var grid = new System.Drawing.Pen(
                    System.Drawing.Color.FromArgb(25, 0, 0, 0), 0.5f);
                g.DrawRectangle(grid, wx, wy, TileSize, TileSize);
            }
        }
    }

    private static void DrawTileDetail(System.Drawing.Graphics g, TileType type, float x, float y)
    {
        switch (type)
        {
            case TileType.Water:
                // Garis ombak
                using (var wave = new System.Drawing.Pen(
                    System.Drawing.Color.FromArgb(80, 100, 160, 220), 1.5f))
                {
                    g.DrawLine(wave, x + 6, y + 14, x + 18, y + 14);
                    g.DrawLine(wave, x + 22, y + 22, x + 38, y + 22);
                    g.DrawLine(wave, x + 8, y + 32, y + 24, y + 32); // intentional
                }
                break;

            case TileType.Tree:
                // Batang
                using (var trunk = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(120, 80, 40)))
                    g.FillRectangle(trunk, x + 18, y + 28, 12, 20);
                // Kanopi
                using (var canopy = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(50, 140, 60)))
                    g.FillEllipse(canopy, x + 4, y + 2, 40, 32);
                using (var canopy2 = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(70, 170, 80)))
                    g.FillEllipse(canopy2, x + 8, y + 6, 32, 24);
                break;

            case TileType.Flower:
                // Bunga kecil acak
                using (var petal = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(230, 80, 120)))
                {
                    g.FillEllipse(petal, x + 10, y + 18, 8, 8);
                    g.FillEllipse(petal, x + 26, y + 10, 8, 8);
                    g.FillEllipse(petal, x + 30, y + 28, 8, 8);
                }
                break;

            case TileType.Path:
                // Batu kerikil kecil
                using (var pebble = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(160, 140, 100)))
                {
                    g.FillEllipse(pebble, x + 8,  y + 10, 6, 4);
                    g.FillEllipse(pebble, x + 28, y + 26, 5, 4);
                    g.FillEllipse(pebble, x + 16, y + 34, 7, 4);
                }
                break;

            case TileType.Stone:
                // Arsir batu
                using (var crack = new System.Drawing.Pen(
                    System.Drawing.Color.FromArgb(60, 100, 96, 92), 1.5f))
                {
                    g.DrawLine(crack, x + 10, y + 10, x + 22, y + 20);
                    g.DrawLine(crack, x + 30, y + 15, x + 38, y + 30);
                }
                break;

            case TileType.House:
                // Atap
                using (var roof = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(180, 60, 50)))
                    g.FillPolygon(roof, new[]
                    {
                        new System.Drawing.PointF(x + 4, y + 20),
                        new System.Drawing.PointF(x + 44, y + 20),
                        new System.Drawing.PointF(x + 24, y + 4),
                    });
                // Pintu
                using (var door = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(100, 60, 30)))
                    g.FillRectangle(door, x + 18, y + 30, 12, 18);
                break;
        }
    }

    // ============================================================
    //  DEFAULT MAP — jika tidak ada file .map
    // ============================================================

    /// <summary>
    /// Peta default built-in (16×12 tile).
    /// Format: setiap karakter adalah satu tile sesuai enum TileType.
    /// </summary>
    public static readonly string[] DefaultMapLines = {
        "################",
        "#..............#",
        "#..TTT...FFF...#",
        "#..TTT.........#",
        "#.....,,,......#",
        "#.....,,,..XX..#",
        "#.....,,,..XX..#",
        "#..........XX..#",
        "#.....H........#",
        "#..TTT.....~~~~#",
        "#..TTT.....~~~~#",
        "################",
    };
}
