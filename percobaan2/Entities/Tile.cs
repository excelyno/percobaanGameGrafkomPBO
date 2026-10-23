using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FarmGame.Entities;

public enum SoilState
{
    Grass,
    Tilled,
    Watered
}

/// <summary>
/// Petak lahan farming RPG.
/// Menangani status tanah, tanaman, dan rendering visual.
/// Koordinat Bounds menggunakan world-space.
/// </summary>
public class Tile
{
    public Rectangle Bounds { get; }

    public SoilState Soil { get; set; } = SoilState.Grass;

    public Crop? Crop { get; set; }

    private static readonly Color SoilOutline =
        Color.FromArgb(67, 43, 35);

    private static readonly Color DrySoil =
        Color.FromArgb(137, 91, 54);

    private static readonly Color DrySoilLight =
        Color.FromArgb(173, 120, 72);

    private static readonly Color WetSoil =
        Color.FromArgb(93, 72, 64);

    private static readonly Color WetSoilLight =
        Color.FromArgb(126, 99, 82);

    public Tile(Rectangle bounds)
    {
        Bounds = bounds;
    }

    public bool Plant(Crop crop)
    {
        if (Soil == SoilState.Grass || Crop != null)
            return false;

        crop.SetPosition(
            Bounds.X + Bounds.Width / 2f,
            Bounds.Bottom - 8
        );

        Crop = crop;
        return true;
    }

    public Crop? RemoveCrop()
    {
        Crop? crop = Crop;
        Crop = null;
        return crop;
    }

    /// <summary>
    /// Mengubah tanah menjadi tanah tercangkul.
    /// Tidak mencabut tanaman yang sudah ada.
    /// </summary>
    public bool Till()
    {
        if (Crop != null)
            return false;

        Soil = SoilState.Tilled;
        return true;
    }

    /// <summary>
    /// Menyiram tanah yang sudah dicangkul.
    /// </summary>
    public bool Water()
    {
        if (Soil == SoilState.Grass)
            return false;

        Soil = SoilState.Watered;
        return true;
    }

    /// <summary>
    /// Mengembalikan tanah ke kondisi rumput.
    /// Petak berisi tanaman tidak dapat direset.
    /// </summary>
    public bool ResetSoil()
    {
        if (Crop != null)
            return false;

        Soil = SoilState.Grass;
        return true;
    }

    /// <summary>
    /// Menggambar visual petak lahan.
    /// Panggil saat rendering world-space.
    /// </summary>
    public void Draw(Graphics g)
    {
        GraphicsState state = g.Save();

        try
        {
            g.SmoothingMode = SmoothingMode.None;

            float x = Bounds.X;
            float y = Bounds.Y;
            float w = Bounds.Width;
            float h = Bounds.Height;

            if (Soil == SoilState.Grass)
            {
                DrawGrass(g, x, y, w, h);
            }
            else
            {
                DrawSoil(g, x, y, w, h, Soil);;
            }
        }
        finally
        {
            g.Restore(state);
        }
    }

    private static void DrawGrass(
        Graphics g, float x, float y, float w, float h)
    {
        Fill(g, Color.FromArgb(83, 145, 65), x, y, w, h);

        Fill(g, Color.FromArgb(106, 174, 75),
            x + 3, y + 3, w - 6, 3);

        Fill(g, Color.FromArgb(64, 120, 54),
            x + 3, y + h - 5, w - 6, 2);

        // Detail kecil berbentuk pixel.
        Fill(g, Color.FromArgb(128, 185, 89),
            x + w * 0.25f, y + h * 0.30f, 3, 2);

        Fill(g, Color.FromArgb(55, 112, 48),
            x + w * 0.68f, y + h * 0.65f, 3, 2);
    }

    private static void DrawSoil(
        Graphics g, float x, float y, float w, float h,
        SoilState soil)
    {
       Color baseColor = soil == SoilState.Watered
        ? WetSoil
        : DrySoil;

        Color highlight = soil == SoilState.Watered
        ? WetSoilLight
        : DrySoilLight;

        // Outline dan permukaan tanah.
        Fill(g, SoilOutline, x, y, w, h);
        Fill(g, baseColor, x + 2, y + 2, w - 4, h - 4);

        // Pinggiran atas untuk memberi ilusi elevasi.
        Fill(g, highlight, x + 3, y + 3, w - 6, 3);

        // Alur cangkul.
        using var groove = new Pen(
    soil == SoilState.Watered
        ? Color.FromArgb(100, 49, 47, 48)
        : Color.FromArgb(100, 79, 48, 32), 2f);

        using var ridge = new Pen(
            Color.FromArgb(90, 230, 175, 110), 1f);

        for (float yy = y + 10; yy < y + h - 4; yy += 10)
        {
            g.DrawLine(groove, x + 4, yy, x + w - 4, yy);
            g.DrawLine(ridge, x + 4, yy + 3, x + w - 4, yy + 3);
        }

        if (soil == SoilState.Watered)
        {
            // Pantulan air kecil pada permukaan tanah basah.
            Fill(g, Color.FromArgb(155, 151, 190, 211),
                x + w * 0.25f, y + h * 0.28f, 7, 2);

            Fill(g, Color.FromArgb(115, 151, 190, 211),
                x + w * 0.62f, y + h * 0.65f, 5, 2);
        }

        // Tepi bawah lebih gelap agar petak terasa berbentuk.
        Fill(g, Color.FromArgb(90, 25, 18, 18),
            x + 2, y + h - 3, w - 4, 2);
    }

    private static void Fill(
        Graphics g, Color color,
        float x, float y, float w, float h)
    {
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, x, y, w, h);
    }
}
