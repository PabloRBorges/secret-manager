namespace SecretManager.Storage;

public sealed record RemovableDrive(string RootPath, string Label, long FreeBytes)
{
    public string Display => string.IsNullOrWhiteSpace(Label)
        ? $"{RootPath}  ({FreeBytes / (1024 * 1024)} MB livres)"
        : $"{RootPath}  {Label}  ({FreeBytes / (1024 * 1024)} MB livres)";
}

/// <summary>Enumera pendrives / drives removiveis disponiveis para backup.</summary>
public static class RemovableDrives
{
    public static IReadOnlyList<RemovableDrive> List()
    {
        var result = new List<RemovableDrive>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (d.DriveType == DriveType.Removable && d.IsReady)
                    result.Add(new RemovableDrive(d.RootDirectory.FullName, d.VolumeLabel, d.AvailableFreeSpace));
            }
            catch
            {
                // drive sumiu / sem permissao — ignora
            }
        }
        return result;
    }
}
