using SecretManager.UI;

namespace SecretManager;

internal static class Program
{
    private const string MutexName = "SecretManager.SingleInstance.9F2A1C";

    [STAThread]
    private static void Main()
    {
        // Instancia unica: se ja estiver rodando no tray, apenas sai.
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isNew);
        if (!isNew) return;

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        using var ctx = new TrayApplicationContext();
        Application.Run(ctx);
    }
}
