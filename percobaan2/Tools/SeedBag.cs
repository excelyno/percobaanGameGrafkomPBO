using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Tools;

/// <summary>
/// Kantong bibit: menanam crop ke tile yang sudah dicangkul.
/// Menggunakan Factory Function (<see cref="Func{Crop}"/>) agar bisa membuat
/// berbagai jenis tanaman tanpa coupling ke implementasi spesifik.
/// </summary>
public class SeedBag : Tool
{
    private readonly string _name;
    private readonly int _cost;
    private readonly Func<Crop> _factory; // Factory pattern sederhana

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
