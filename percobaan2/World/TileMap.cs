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
    int c0 = Math.Max(0,
        (int)((cam.X - cam.ViewW / cam.Zoom / 2f) / TileSize) - 1);
    int r0 = Math.Max(0,
        (int)((cam.Y - cam.ViewH / cam.Zoom / 2f) / TileSize) - 1);

    int c1 = Math.Min(Cols - 1,
        c0 + (int)(cam.ViewW / cam.Zoom / TileSize) + 3);
    int r1 = Math.Min(Rows - 1,
        r0 + (int)(cam.ViewH / cam.Zoom / TileSize) + 3);

    for (int r = r0; r <= r1; r++)
    {
        for (int c = c0; c <= c1; c++)
        {
            MapCell cell = _cells[c, r];

            float x = c * TileSize;
            float y = r * TileSize;

            // Warna dasar permukaan.
            using (var brush = new System.Drawing.SolidBrush(cell.BaseColor))
                g.FillRectangle(brush, x, y, TileSize, TileSize);

            DrawTileDetail(g, cell.Type, x, y, c, r);

            // Bevel: bagian atas lebih terang, bagian bawah lebih gelap.
            using (var light = new System.Drawing.Pen(
                System.Drawing.Color.FromArgb(24, 255, 255, 255), 1f))
            using (var shadow = new System.Drawing.Pen(
                System.Drawing.Color.FromArgb(32, 0, 0, 0), 1f))
            {
                g.DrawLine(light, x, y, x + TileSize - 1, y);
                g.DrawLine(light, x, y, x, y + TileSize - 1);

                g.DrawLine(shadow,
                    x, y + TileSize - 1,
                    x + TileSize - 1, y + TileSize - 1);

                g.DrawLine(shadow,
                    x + TileSize - 1, y,
                    x + TileSize - 1, y + TileSize - 1);
            }
        }
    }
}

private static void DrawTileDetail(
    System.Drawing.Graphics g,
    TileType type,
    float x,
    float y,
    int col,
    int row)
{
    // Pola berdasarkan koordinat, bukan random setiap frame.
    int seed = Math.Abs((col * 73856093) ^ (row * 19349663));

    switch (type)
    {
        case TileType.Grass:
        {
            // Variasi rumput kecil.
            using var dark = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(45, 39, 90, 34));

            using var light = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(55, 200, 235, 115));

            for (int i = 0; i < 4; i++)
            {
                int px = 5 + (seed >> (i * 3) & 31);
                int py = 5 + (seed >> (i * 4 + 2) & 31);

                g.FillRectangle(dark, x + px, y + py, 3, 2);

                if (i % 2 == 0)
                    g.FillRectangle(light, x + px + 1, y + py - 2, 2, 2);
            }

            break;
        }

        case TileType.Water:
        {
            using var deep = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(45, 20, 65, 145));

            using var shine = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(100, 130, 195, 245));

            using var soft = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(65, 170, 215, 250));

            g.FillRectangle(deep, x + 1, y + 35, TileSize - 2, 11);

            g.FillRectangle(shine, x + 6, y + 10, 13, 2);
            g.FillRectangle(soft, x + 23, y + 19, 16, 2);
            g.FillRectangle(shine, x + 11, y + 29, 8, 2);
            g.FillRectangle(soft, x + 30, y + 38, 10, 2);

            break;
        }

        case TileType.Path:
        {
            using var pebble = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(115, 115, 86));

            using var pebbleLight = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(205, 183, 139));

            g.FillRectangle(pebble, x + 8, y + 11, 4, 3);
            g.FillRectangle(pebbleLight, x + 9, y + 10, 3, 2);

            g.FillRectangle(pebble, x + 29, y + 27, 5, 3);
            g.FillRectangle(pebbleLight, x + 30, y + 26, 3, 2);

            g.FillRectangle(pebble, x + 17, y + 38, 4, 2);

            break;
        }

        case TileType.Farm:
        {
            using var groove = new System.Drawing.Pen(
                System.Drawing.Color.FromArgb(90, 66, 39), 2f);

            using var ridge = new System.Drawing.Pen(
                System.Drawing.Color.FromArgb(165, 119, 68), 2f);

            for (int yy = 9; yy < TileSize - 4; yy += 10)
            {
                g.DrawLine(groove,
                    x + 4, y + yy,
                    x + TileSize - 4, y + yy);

                g.DrawLine(ridge,
                    x + 4, y + yy + 3,
                    x + TileSize - 4, y + yy + 3);
            }

            break;
        }

        case TileType.Tree:
        {
            // Bayangan pohon di tanah.
            using var shadow = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(65, 20, 35, 24));

            g.FillEllipse(shadow, x + 3, y + 28, 43, 17);

            // Batang.
            using var trunkDark = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(95, 54, 35));

            using var trunk = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(145, 91, 48));

            g.FillRectangle(trunkDark, x + 17, y + 21, 15, 26);
            g.FillRectangle(trunk, x + 19, y + 22, 10, 24);

            // Kanopi berlapis.
            using var leafDark = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(30, 94, 43));

            using var leafMid = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(42, 133, 54));

            using var leafLight = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(76, 165, 68));

            g.FillEllipse(leafDark, x + 1, y + 2, 46, 34);
            g.FillEllipse(leafMid, x + 5, y + 2, 37, 29);
            g.FillEllipse(leafLight, x + 9, y + 5, 23, 16);

            // Highlight blok.
            g.FillRectangle(leafLight, x + 12, y + 8, 7, 4);

            break;
        }

        case TileType.Flower:
        {
            using var stem = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(45, 103, 43));

            using var petal = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(245, 82, 126));

            using var center = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(255, 220, 100));

            int fx = 8 + (seed & 15);
            int fy = 9 + ((seed >> 5) & 18);

            g.FillRectangle(stem, x + fx + 3, y + fy + 4, 2, 8);
            g.FillRectangle(petal, x + fx, y + fy, 8, 8);
            g.FillRectangle(center, x + fx + 3, y + fy + 3, 3, 3);

            break;
        }

        case TileType.Stone:
        {
            using var shade = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(65, 45, 45, 45));

            using var highlight = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(65, 230, 225, 215));

            g.FillRectangle(shade, x + 7, y + 8, 32, 30);
            g.FillRectangle(highlight, x + 8, y + 8, 29, 3);
            g.FillRectangle(highlight, x + 8, y + 8, 3, 25);

            break;
        }

        case TileType.House:
        {
            // Dinding.
            using var wall = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(210, 164, 112));

            using var wallShade = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(145, 103, 71));

            using var roof = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(150, 48, 46));

            using var roofLight = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(190, 69, 59));

            g.FillRectangle(wall, x + 7, y + 19, 34, 28);
            g.FillRectangle(wallShade, x + 34, y + 21, 7, 26);

            g.FillPolygon(roof, new[]
            {
                new System.Drawing.PointF(x + 2, y + 21),
                new System.Drawing.PointF(x + 24, y + 3),
                new System.Drawing.PointF(x + 46, y + 21)
            });

            g.FillPolygon(roofLight, new[]
            {
                new System.Drawing.PointF(x + 6, y + 19),
                new System.Drawing.PointF(x + 24, y + 5),
                new System.Drawing.PointF(x + 24, y + 19)
            });

            using var door = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(90, 53, 34));

            g.FillRectangle(door, x + 18, y + 30, 12, 17);

            break;
        }
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
