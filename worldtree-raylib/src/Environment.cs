// src/Environment.cs
using Raylib_cs;
using System.Numerics;

namespace WorldTree;

/// <summary>
/// A game environment (one room/map).
/// Corresponds to worldtree/environment.py.
/// Entity positions are in world (map) coordinates.
/// </summary>
public class Environment
{
    // Static maps loaded once at startup
    public static Dictionary<int, Dictionary<string, MapInfo>> Regions = new();
    // {region: {room: song_filename}}
    public static Dictionary<int, Dictionary<string, string>> SongsByRoom = new();
    // {region: {room: Color}}
    public static Dictionary<int, Dictionary<string, Color>> BgColorsByRoom = new();

    public string Name { get; }
    public int Region { get; }
    public int Width { get; }   // in tiles
    public int Height { get; }  // in tiles
    public Color BgColor { get; }

    // Tile grid indexed as [col][row]
    private Tile[][] _grid;

    // Entity groups (world coordinates)
    public List<Character> EnemyGroup { get; } = new();
    public List<Character> DyingAnimationGroup { get; } = new();
    public List<Powerup> ItemGroup { get; } = new();
    public List<Projectile> HeroProjectileGroup { get; } = new();
    public List<Projectile> EnemyProjectileGroup { get; } = new();

    public Environment(string mapName, int region)
    {
        Name = mapName;
        Region = region;
        var mapInfo = Regions[region][mapName];
        Width = mapInfo.Width;
        Height = mapInfo.Height;
        BgColor = BgColorsByRoom.TryGetValue(region, out var roomColors)
                  && roomColors.TryGetValue(mapName, out var c) ? c : Color.Black;

        _grid = new Tile[Width][];
        for (int col = 0; col < Width; col++)
            _grid[col] = new Tile[Height];

        BuildTileGrid(mapInfo);
    }

    private void BuildTileGrid(MapInfo mapInfo)
    {
        var imageCache = new Dictionary<string, Texture2D>();
        var areas = new Dictionary<int, List<(int col, int row)>>();

        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                int tileId = mapInfo.Layout[row][col];
                int boundByte = mapInfo.Bounds[row][col];
                int mapCode = mapInfo.Mapcodes[row][col];

                if (tileId == 0)
                {
                    _grid[col][row] = Tile.Empty;
                }
                else
                {
                    string imageName = $"{mapInfo.Tileset}-{tileId}.png";
                    if (!imageCache.TryGetValue(imageName, out var tex))
                    {
                        tex = TextureCache.LoadTile(
                            Path.Combine(GameConstants.TileDir, imageName));
                        imageCache[imageName] = tex;
                    }
                    _grid[col][row] = Tile.WithImage(tex, (byte)boundByte);
                }

                if (mapCode != 0)
                    SpawnMapCode(mapCode, col, row, areas);
            }
        }

        CreateAreas(areas);
    }

    // Enemy/item spawn codes — mirrors ENEMIES and ITEMS dicts in environment.py
    private static readonly HashSet<int> AreaCodes = new() { 254, 255 };

    private void SpawnMapCode(int mapCode, int col, int row,
        Dictionary<int, List<(int, int)>> areas)
    {
        if (AreaCodes.Contains(mapCode))
        {
            areas.TryAdd(mapCode, new List<(int, int)>());
            areas[mapCode].Add((col, row));
            return;
        }
        var pos = (col, row);
        Character? enemy = mapCode switch
        {
            1  => new Enemies.Beaver(this, pos),
            2  => new Enemies.Dragonfly(this, pos),
            3  => new Enemies.BoomBug(this, pos),
            4  => new Enemies.Shooter(this, pos),
            5  => new Enemies.BugPipe(this, pos),
            6  => new Enemies.PipeBug(this, pos),
            7  => new Enemies.Batzor(this, pos),
            8  => new Enemies.BiterPipe(this, pos),
            9  => new Enemies.Biter(this, pos),
            10 => new Enemies.Slug(this, pos),
            11 => new Enemies.Baron(this, pos),
            _ => null
        };
        if (enemy != null) { EnemyGroup.Add(enemy); return; }

        Powerup? item = mapCode switch
        {
            129 => new Powerups.HealthBoost(this, pos),
            130 => new Powerups.DoubleJump(this, pos),
            131 => new Powerups.MoreSeeds(this, pos),
            _ => null
        };
        if (item != null) { ItemGroup.Add(item); return; }

        throw new Exception($"Unknown mapcode: {mapCode}");
    }

    private void CreateAreas(Dictionary<int, List<(int col, int row)>> areaDict)
    {
        foreach (var (mapCode, coords) in areaDict)
        {
            int i = 0;
            while (i < coords.Count)
            {
                var start = coords[i];
                int width = 1;
                while (i + width < coords.Count
                       && coords[i + width].row == start.row
                       && coords[i + width].col == start.col + width)
                    width++;
                Powerup area = mapCode switch
                {
                    254 => new Powerups.Spike(this, start, width),
                    255 => new Powerups.Lava(this, start, width),
                    _   => throw new Exception($"Unknown area code: {mapCode}")
                };
                ItemGroup.Add(area);
                i += width;
            }
        }
    }

    public Tile GetTile(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height)
            return Tile.Empty;
        return _grid[col][row];
    }

    // World rect for a tile (used for collision)
    public Rectangle RectForTile(int col, int row) =>
        new Rectangle(col * GameConstants.TileWidth, row * GameConstants.TileHeight,
                      GameConstants.TileWidth - 1, GameConstants.TileHeight - 1);

    public (int col, int row) TileIndexForPoint(float x, float y) =>
        ((int)MathF.Floor(x / GameConstants.TileWidth),
         (int)MathF.Floor(y / GameConstants.TileHeight));

    public bool IsOutsideMap(Rectangle rect)
    {
        var (col, row) = TileIndexForPoint(rect.CenterX(), rect.CenterY());
        return col < 0 || col >= Width || row < 0 || row >= Height;
    }
}
