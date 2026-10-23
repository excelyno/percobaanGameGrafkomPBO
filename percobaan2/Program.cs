using FarmGame.UI;

namespace FarmGame;

/// <summary>
/// Entry point aplikasi.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm());
    }
}
