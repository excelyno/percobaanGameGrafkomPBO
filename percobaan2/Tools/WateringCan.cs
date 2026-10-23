using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Tools;

/// <summary>
/// Penyiram air: mengubah tanah cangkulan (Tilled) menjadi basah (Watered).
/// </summary>
public class WateringCan : Tool
{
    public override string Label => "Penyiram";

    public override void Use(Tile tile, IGameContext ctx)
    {
        if (tile.Soil == SoilState.Tilled)
            tile.Soil = SoilState.Watered;
        else if (tile.Soil == SoilState.Grass)
            ctx.Toast("Cangkul dulu tanahnya.");
    }
}
