using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Tools;

/// <summary>
/// Sabit: memulai minigame panen jika tanaman sudah matang.
/// </summary>
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
