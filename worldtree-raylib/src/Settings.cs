using System.Text.Json;

namespace WorldTree;

public class Settings
{
    public string Resolution { get; set; } = "1280x720";
    public bool Fullscreen { get; set; } = false;

    private const string FilePath = "settings.json";

    // Single source of truth for valid resolutions, in display order.
    public static readonly Dictionary<string, (int w, int h)> ResolutionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["1280x720"]  = (1280,  720),
            ["1920x1080"] = (1920, 1080),
            ["2560x1440"] = (2560, 1440),
        };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<Settings>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (s != null && ResolutionMap.ContainsKey(s.Resolution))
                    return s;
            }
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
            };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, options));
        }
        catch { }
    }

    // Returns window dimensions for the current Resolution string.
    // Falls back to 1280x720 if the string is not in ResolutionMap.
    public (int w, int h) WindowSize() =>
        ResolutionMap.TryGetValue(Resolution, out var s) ? s : (1280, 720);

    // Returns the resolution key that comes after the current one (wraps around).
    public string NextResolution()
    {
        var keys = ResolutionMap.Keys.ToList();
        int idx = keys.FindIndex(k => string.Equals(k, Resolution, StringComparison.OrdinalIgnoreCase));
        return keys[(idx + 1) % keys.Count];
    }

    // Returns the resolution key that comes before the current one (wraps around).
    public string PrevResolution()
    {
        var keys = ResolutionMap.Keys.ToList();
        int idx = keys.FindIndex(k => string.Equals(k, Resolution, StringComparison.OrdinalIgnoreCase));
        return keys[(idx - 1 + keys.Count) % keys.Count];
    }
}
