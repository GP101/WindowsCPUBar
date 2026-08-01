using System.Text.Json;

namespace WindowsCpuBar;

internal sealed class AppSettings
{
    public int UpdateIntervalMs { get; set; } = 1000;
    public int HistorySeconds { get; set; } = 60;
    public int BarColorArgb { get; set; } = Color.FromArgb(0, 120, 215).ToArgb();
    public int GpuBarColorArgb { get; set; } = Color.FromArgb(16, 185, 129).ToArgb();
    public bool ShowText { get; set; } = true;
    public int TopProcessCount { get; set; } = 14;

    public int GetHistoryCapacity()
    {
        var interval = Math.Max(200, UpdateIntervalMs);
        return Math.Clamp(HistorySeconds * 1000 / interval, 10, 600);
    }

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsCpuBar",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = FilePath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore persistence errors.
        }
    }

    public Color BarColor => Color.FromArgb(BarColorArgb);

    public Color GpuBarColor => Color.FromArgb(GpuBarColorArgb);
}
