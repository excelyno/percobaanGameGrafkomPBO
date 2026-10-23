using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Tools;

// ============================================================
//  ALAT: Abstraction + Polymorphism (Strategy pattern)
// ============================================================

/// <summary>
/// Base class abstrak untuk semua alat yang bisa dipakai pemain.
/// Menggunakan Strategy pattern — tiap alat punya perilaku "Use" sendiri.
/// </summary>
public abstract class Tool
{
    public abstract string Label { get; }
    public abstract void Use(Tile tile, IGameContext ctx);
}
