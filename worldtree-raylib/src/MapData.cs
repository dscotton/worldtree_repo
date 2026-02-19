// src/MapData.cs
namespace WorldTree;

public class MapInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Tileset { get; set; } = "";
    public List<List<int>> Layout { get; set; } = [];
    public List<List<int>> Bounds { get; set; } = [];
    public List<List<int>> Mapcodes { get; set; } = [];
}

public class TransitionInfo
{
    public int First { get; set; }
    public int Last { get; set; }
    public int Region { get; set; }
    public string Dest { get; set; } = "";
    public int Offset { get; set; }
}

public enum TransitionDirection { Left, Right, Up, Down }
