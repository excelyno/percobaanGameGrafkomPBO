namespace FarmGame.Core;

/// <summary>
/// Base class untuk semua objek dalam game.
/// Encapsulation: posisi X/Y hanya bisa diubah lewat SetPosition().
/// </summary>
public abstract class GameObject
{
    public float X { get; private set; }
    public float Y { get; private set; }

    public void SetPosition(float x, float y)
    {
        X = x;
        Y = y;
    }
}
