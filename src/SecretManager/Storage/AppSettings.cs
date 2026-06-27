using System.Text.Json;

namespace SecretManager.Storage;

/// <summary>Preferencias nao sensiveis do app (gravadas em texto claro, sem segredos).</summary>
public sealed class AppSettings
{
    public int AutoLockMinutes { get; set; } = 5;
    public int ClipboardClearSeconds { get; set; } = 30;
    public bool StartWithWindows { get; set; } = false;
    public bool BackupOnSave { get; set; } = false;
    public string? BackupDrivePath { get; set; }

    private static string Path => System.IO.Path.Combine(VaultStore.AppDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(Path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path)) ?? new AppSettings();
        }
        catch { /* config corrompida — volta ao default */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best-effort */ }
    }
}
