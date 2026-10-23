using System.Drawing;

namespace FarmGame.Core;

/// <summary>
/// Kontrak untuk semua objek yang bisa digambar ke layar.
/// </summary>
public interface IRenderable
{
    void Draw(Graphics g, float time);
}

/// <summary>
/// Kontrak untuk konteks game yang dipakai oleh Tool.
/// Memisahkan logika Tool dari implementasi Form (Dependency Inversion).
/// </summary>
public interface IGameContext
{
    void Toast(string message);
    void BeginHarvest(FarmGame.Entities.Tile tile);
    bool TrySpend(int amount);
}
