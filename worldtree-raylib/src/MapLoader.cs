// src/MapLoader.cs
using System.Text.Json;

namespace WorldTree;

public static class MapLoader
{
    public static Dictionary<string, MapInfo> LoadRegion(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);
        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        var result = new Dictionary<string, MapInfo>();
        foreach (var (key, val) in raw)
        {
            result[key] = new MapInfo
            {
                Width    = val.GetProperty("width").GetInt32(),
                Height   = val.GetProperty("height").GetInt32(),
                Tileset  = val.GetProperty("tileset").GetString()!,
                Layout   = DeserializeIntGrid(val.GetProperty("layout")),
                Bounds   = DeserializeIntGrid(val.GetProperty("bounds")),
                Mapcodes = DeserializeIntGrid(val.GetProperty("mapcodes")),
            };
        }
        return result;
    }

    public static Dictionary<int, Dictionary<string, Dictionary<TransitionDirection, List<TransitionInfo>>>>
        LoadTransitions(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);
        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        var result = new Dictionary<int, Dictionary<string, Dictionary<TransitionDirection, List<TransitionInfo>>>>();
        foreach (var (regionStr, regionVal) in raw)
        {
            int region = int.Parse(regionStr);
            result[region] = new();
            foreach (var room in regionVal.EnumerateObject())
            {
                result[region][room.Name] = new();
                foreach (var dir in room.Value.EnumerateObject())
                {
                    var dirEnum = dir.Name switch {
                        "LEFT"  => TransitionDirection.Left,
                        "RIGHT" => TransitionDirection.Right,
                        "UP"    => TransitionDirection.Up,
                        "DOWN"  => TransitionDirection.Down,
                        _ => throw new Exception($"Unknown direction: {dir.Name}")
                    };
                    var list = new List<TransitionInfo>();
                    foreach (var t in dir.Value.EnumerateArray())
                        list.Add(new TransitionInfo {
                            First  = t.GetProperty("first").GetInt32(),
                            Last   = t.GetProperty("last").GetInt32(),
                            Region = t.GetProperty("region").GetInt32(),
                            Dest   = t.GetProperty("dest").GetString()!,
                            Offset = t.GetProperty("offset").GetInt32(),
                        });
                    result[region][room.Name][dirEnum] = list;
                }
            }
        }
        return result;
    }

    private static List<List<int>> DeserializeIntGrid(JsonElement el)
    {
        var rows = new List<List<int>>();
        foreach (var row in el.EnumerateArray())
        {
            var r = new List<int>();
            foreach (var cell in row.EnumerateArray())
                r.Add(cell.GetInt32());
            rows.Add(r);
        }
        return rows;
    }
}
