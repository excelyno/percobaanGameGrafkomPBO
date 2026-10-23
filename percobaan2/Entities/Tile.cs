namespace FarmGame.Entities;

// ============================================================
//  TILE / LAHAN
// ============================================================

/// <summary>Status tanah pada sebuah tile lahan.</summary>
public enum SoilState { Grass, Tilled, Watered }

/// <summary>
/// Mewakili satu petak lahan di grid farm.
/// Mengelola kondisi tanah dan tanaman yang ada di dalamnya.
/// </summary>
public class Tile
{
    public Rectangle Bounds { get; }
    public SoilState Soil { get; set; } = SoilState.Grass;
    public Crop? Crop { get; private set; }

    public Tile(Rectangle bounds) => Bounds = bounds;

    /// <summary>Tanam crop ke tile ini. Hanya berhasil jika sudah dicangkul dan belum ada tanaman.</summary>
    public bool Plant(Crop crop)
    {
        if (Soil == SoilState.Grass || Crop != null) return false;
        crop.SetPosition(Bounds.X + Bounds.Width / 2f, Bounds.Bottom - 10);
        Crop = crop;
        return true;
    }

    /// <summary>Ambil dan hapus crop dari tile ini (saat panen).</summary>
    public Crop? RemoveCrop()
    {
        var c = Crop;
        Crop = null;
        return c;
    }
}
