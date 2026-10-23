using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Tools;

/// <summary>
/// Cangkul: mengubah tanah rumput (Grass) menjadi tanah cangkulan (Tilled).
/// </summary>
public class HoeTool : Tool
{
    public override string Label => "Cangkul";

    public override void Use(Tile tile, IGameContext ctx)
    {
        if (tile.Soil == SoilState.Grass)
            tile.Soil = SoilState.Tilled;
        else
            ctx.Toast("Tanah ini sudah dicangkul.");
    }
}
