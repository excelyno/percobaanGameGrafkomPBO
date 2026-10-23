namespace FarmGame.Core;

// ============================================================
//  INPUT STATE — WASD + Gamepad (XInput via WinForms joystick)
// ============================================================

/// <summary>
/// Snapshot input satu frame. Dikumpulkan di awal Update(),
/// lalu disebarkan ke semua sistem — tidak ada coupling ke Form.
/// </summary>
public class InputState
{
    // ---- Keyboard ----
    public bool Left   { get; private set; }
    public bool Right  { get; private set; }
    public bool Up     { get; private set; }
    public bool Down   { get; private set; }
    public bool Sprint { get; private set; }   // Shift
    public bool Interact { get; private set; } // E / Enter
    public bool Build  { get; private set; }   // B toggle build mode
    public bool Cancel { get; private set; }   // Escape

    // ---- Gamepad (analog) ----
    /// <summary>-1..1 horizontal (gamepad left stick atau keyboard)</summary>
    public float AxisX { get; private set; }
    /// <summary>-1..1 vertical</summary>
    public float AxisY { get; private set; }

    // ---- Mouse (untuk build mode) ----
    public System.Drawing.Point MousePos   { get; private set; }
    public bool MouseLeft  { get; private set; }
    public bool MouseRight { get; private set; }

    // ---- Edge detection (hanya 1 frame) ----
    public bool JustBuild    { get; private set; }
    public bool JustInteract { get; private set; }
    public bool JustCancel   { get; private set; }

    private bool _prevBuild, _prevInteract, _prevCancel;

    /// <summary>
    /// Ambil snapshot dari keys yang sedang ditekan.
    /// Panggil sekali di awal tiap frame.
    /// </summary>
    public void Poll(HashSet<Keys> held, System.Drawing.Point mousePos,
                     bool mouseLBtn, bool mouseRBtn,
                     float gamepadX = 0f, float gamepadY = 0f)
    {
        Left     = held.Contains(Keys.A) || held.Contains(Keys.Left);
        Right    = held.Contains(Keys.D) || held.Contains(Keys.Right);
        Up       = held.Contains(Keys.W) || held.Contains(Keys.Up);
        Down     = held.Contains(Keys.S) || held.Contains(Keys.Down);
        Sprint   = held.Contains(Keys.ShiftKey) || held.Contains(Keys.LShiftKey);
        Interact = held.Contains(Keys.E) || held.Contains(Keys.Enter);
        Build    = held.Contains(Keys.B);
        Cancel   = held.Contains(Keys.Escape);

        // Keyboard axis (normalisasi diagonal)
        float kx = (Right ? 1f : 0f) - (Left ? 1f : 0f);
        float ky = (Down  ? 1f : 0f) - (Up   ? 1f : 0f);
        float klen = MathF.Sqrt(kx * kx + ky * ky);
        if (klen > 0) { kx /= klen; ky /= klen; }

        // Gamepad override jika ada input analog
        float glen = MathF.Sqrt(gamepadX * gamepadX + gamepadY * gamepadY);
        if (glen > 0.15f) // dead zone
        {
            AxisX = gamepadX;
            AxisY = gamepadY;
        }
        else
        {
            AxisX = kx;
            AxisY = ky;
        }

        MousePos   = mousePos;
        MouseLeft  = mouseLBtn;
        MouseRight = mouseRBtn;

        // Edge detection
        JustBuild    = Build    && !_prevBuild;
        JustInteract = Interact && !_prevInteract;
        JustCancel   = Cancel   && !_prevCancel;

        _prevBuild    = Build;
        _prevInteract = Interact;
        _prevCancel   = Cancel;
    }
}
