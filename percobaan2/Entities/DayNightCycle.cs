namespace FarmGame.Entities;

// ============================================================
//  SIKLUS WAKTU SIANG-MALAM
// ============================================================

/// <summary>
/// Mengatur jam dan hari dalam game.
/// Satu hari game = <see cref="DayLengthSeconds"/> detik nyata.
/// </summary>
public class DayNightCycle
{
    public const float DayLengthSeconds = 24f;

    public int Day { get; private set; } = 1;
    public float Hour { get; private set; } = 6f;

    /// <summary>Event yang ditrigger setiap pergantian hari (jam 00:00 game).</summary>
    public event Action? NewDay;

    public void Update(float dt)
    {
        Hour += dt / DayLengthSeconds * 24f;
        if (Hour >= 24f)
        {
            Hour -= 24f;
            Day++;
            NewDay?.Invoke();
        }
    }

    /// <summary>Loncat ke menjelang tengah malam agar hari segera berganti.</summary>
    public void SkipToNextDay() => Hour = 23.99f;

    /// <summary>
    /// Intensitas cahaya siang: 0 = malam total, 1 = siang terang.
    /// Transisi halus saat fajar (05-07) dan senja (17-19).
    /// </summary>
    public float Daylight
    {
        get
        {
            if (Hour < 5f || Hour > 19f) return 0f;
            if (Hour < 7f) return (Hour - 5f) / 2f;
            if (Hour > 17f) return (19f - Hour) / 2f;
            return 1f;
        }
    }

    /// <summary>True jika matahari sedang di atas (06:00 – 18:00).</summary>
    public bool IsSunUp => Hour >= 6f && Hour < 18f;

    /// <summary>Posisi 0..1 sepanjang busur langit untuk matahari atau bulan.</summary>
    public float ArcT => IsSunUp
        ? (Hour - 6f) / 12f
        : (Hour >= 18f ? Hour - 18f : Hour + 6f) / 12f;
}
