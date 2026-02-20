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
    // Transitions
    public static Dictionary<int, Dictionary<string, Dictionary<TransitionDirection, List<TransitionInfo>>>> AllTransitions = new();

    public string Name { get; }
    public int Region { get; }
    public int Width { get; }   // in tiles
    public int Height { get; }  // in tiles
    public Color BgColor { get; }

    // Tile grid indexed as [col][row]
    private Tile[][] _grid;

    // Entity groups (world coordinates)
    public List<Character> EnemyGroup { get; } = new();
    public List<DyingAnimation> DyingAnimationGroup { get; } = new();
    public List<Powerup> ItemGroup { get; } = new();
    public List<Projectile> HeroProjectileGroup { get; } = new();
    public List<Projectile> EnemyProjectileGroup { get; } = new();

    /// <summary>
    /// Internal constructor for testing. Bypasses asset loading.
    /// </summary>
    internal Environment(int width, int height, Tile[][] grid)
    {
        Name = "TestMap";
        Region = 1;
        Width = width;
        Height = height;
        BgColor = Color.Black;
        _grid = grid;
    }

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

    // Camera state (replaces screen_offset in Python)
    public Vector2 ScreenOffset { get; private set; } = Vector2.Zero;

    public Camera2D MakeCamera() => new Camera2D
    {
        Offset = new Vector2(GameConstants.MapX, GameConstants.MapY),
        Target = ScreenOffset,
        Rotation = 0f,
        Zoom = 1f
    };

    /// <summary>
    /// Update the camera to follow rect (world coords). Returns updated camera.
    /// Replaces Environment.Scroll() in Python — no need to apply scroll_vector to entities
    /// because all entities are already in world coordinates.
    /// </summary>
    public Camera2D Scroll(Camera2D camera, Rectangle rect)
    {
        var offset = ScreenOffset;
        float mapPixelW = Width * GameConstants.TileWidth;
        float mapPixelH = Height * GameConstants.TileHeight;

        if (rect.CenterX() < offset.X + GameConstants.ScrollMarginX && offset.X > 0)
            offset.X = MathF.Max(0, rect.CenterX() - GameConstants.ScrollMarginX);
        else if (rect.CenterX() > offset.X + GameConstants.MapWidth - GameConstants.ScrollMarginX
                 && offset.X + GameConstants.MapWidth < mapPixelW)
            offset.X = MathF.Min(mapPixelW - GameConstants.MapWidth,
                                 rect.CenterX() - (GameConstants.MapWidth - GameConstants.ScrollMarginX));

        if (rect.CenterY() < offset.Y + GameConstants.ScrollMarginY && offset.Y > 0)
            offset.Y = MathF.Max(0, rect.CenterY() - GameConstants.ScrollMarginY);
        else if (rect.CenterY() > offset.Y + GameConstants.MapHeight - GameConstants.ScrollMarginY
                 && offset.Y + GameConstants.MapHeight < mapPixelH)
            offset.Y = MathF.Min(mapPixelH - GameConstants.MapHeight,
                                 rect.CenterY() - (GameConstants.MapHeight - GameConstants.ScrollMarginY));

        ScreenOffset = offset;
        camera.Target = offset;
        return camera;
    }

    /// <summary>Set initial camera offset (e.g. on room transition).</summary>
    public void SetScreenOffset(float x, float y) =>
        ScreenOffset = new Vector2(
            Math.Clamp(x, 0, Width * GameConstants.TileWidth - GameConstants.MapWidth),
            Math.Clamp(y, 0, Height * GameConstants.TileHeight - GameConstants.MapHeight));

    /// <summary>
    /// Draw the visible tiles. Call this inside BeginMode2D / EndMode2D.
    /// </summary>
    public void DrawTiles()
    {
        int firstCol = (int)(ScreenOffset.X / GameConstants.TileWidth);
        int lastCol = firstCol + GameConstants.MapWidth / GameConstants.TileWidth + 1;
        int firstRow = (int)(ScreenOffset.Y / GameConstants.TileHeight);
        int lastRow = firstRow + GameConstants.MapHeight / GameConstants.TileHeight + 1;

        for (int col = firstCol; col <= Math.Min(lastCol, Width - 1); col++)
            for (int row = firstRow; row <= Math.Min(lastRow, Height - 1); row++)
            {
                var tile = _grid[col][row];
                if (tile == Tile.Empty || tile.Image == null) continue;
                Raylib.DrawTexture(tile.Image.Value,
                    col * GameConstants.TileWidth,
                    row * GameConstants.TileHeight,
                    Color.White);
            }
    }

    /// <summary>
    /// Draw solid-edge indicators for all visible tiles. Call inside BeginMode2D/EndMode2D.
    /// Each solid edge is shown as a white line along that side of the tile.
    /// </summary>
    public void DrawDebugBounds()
    {
        int firstCol = (int)(ScreenOffset.X / GameConstants.TileWidth);
        int lastCol  = firstCol + GameConstants.MapWidth  / GameConstants.TileWidth + 1;
        int firstRow = (int)(ScreenOffset.Y / GameConstants.TileHeight);
        int lastRow  = firstRow + GameConstants.MapHeight / GameConstants.TileHeight + 1;

        for (int col = firstCol; col <= Math.Min(lastCol, Width - 1); col++)
        {
            for (int row = firstRow; row <= Math.Min(lastRow, Height - 1); row++)
            {
                var tile = _grid[col][row];
                if (tile.IsEmpty) continue;

                int x  = col * GameConstants.TileWidth;
                int y  = row * GameConstants.TileHeight;
                int x2 = x + GameConstants.TileWidth;
                int y2 = y + GameConstants.TileHeight;

                if (tile.SolidTop)    Raylib.DrawLine(x, y,  x2, y,  Color.White);
                if (tile.SolidBottom) Raylib.DrawLine(x, y2, x2, y2, Color.White);
                if (tile.SolidLeft)   Raylib.DrawLine(x, y,  x,  y2, Color.White);
                if (tile.SolidRight)  Raylib.DrawLine(x2, y, x2, y2, Color.White);
            }
        }
    }

    public bool IsWorldPointVisible(float x, float y) =>
        x >= ScreenOffset.X && x <= ScreenOffset.X + GameConstants.MapWidth &&
        y >= ScreenOffset.Y && y <= ScreenOffset.Y + GameConstants.MapHeight;

    /// <summary>
    /// Check which tile grid cells a world-coordinate rect intersects.
    /// </summary>
    public List<(int col, int row)> TilesForRect(Rectangle rect)
    {
        int left = (int)MathF.Floor(rect.Left() / GameConstants.TileWidth);
        int right = (int)MathF.Floor(rect.Right() / GameConstants.TileWidth);
        int top = (int)MathF.Floor(rect.Top() / GameConstants.TileHeight);
        int bot = (int)MathF.Floor(rect.Bottom() / GameConstants.TileHeight);
        var result = new List<(int, int)>();
        for (int c = left; c <= right; c++)
            for (int r = top; r <= bot; r++)
                result.Add((c, r));
        return result;
    }

    /// <summary>
    /// Attempt a move, blocking per-tile-side solidity. Returns new world rect.
    /// Resolves Y first, then X, so upward movement clears obstacles before
    /// horizontal movement is checked against them.
    /// For player (isPlayer=true), allows moving off map edges for room transitions.
    /// </summary>
    public Rectangle AttemptMove(Rectangle hitbox, (float x, float y) vector, bool isPlayer = false)
    {
        var afterY = MoveY(hitbox, vector.y, isPlayer);
        return MoveX(afterY, vector.x, isPlayer);
    }

    private Rectangle MoveY(Rectangle hitbox, float dy, bool isPlayer)
    {
        if (dy == 0f) return hitbox;
        var dest = hitbox.Move(0, dy);
        float newVY = dy;
        var oldTiles = new HashSet<(int, int)>(TilesForRect(hitbox));
        foreach (var (col, row) in TilesForRect(dest).Where(t => !oldTiles.Contains(t)))
        {
            Tile square;
            if (col < 0 || col >= Width || row < 0 || row >= Height)
                square = isPlayer ? new Tile(null, false, false, false, false)
                                  : new Tile(null, true, true, true, true);
            else
                square = _grid[col][row];

            var tileRect = RectForTile(col, row);

            if (hitbox.Bottom() < tileRect.Top() && square.SolidTop && dest.Bottom() >= tileRect.Top())
                newVY = MathF.Min(newVY, tileRect.Top() - hitbox.Bottom() - 1);
            else if (hitbox.Top() > tileRect.Bottom() && square.SolidBottom && dest.Top() <= tileRect.Bottom())
                newVY = MathF.Max(newVY, tileRect.Bottom() - hitbox.Top() + 1);
        }
        return hitbox.Move(0, newVY);
    }

    private Rectangle MoveX(Rectangle hitbox, float dx, bool isPlayer)
    {
        if (dx == 0f) return hitbox;
        var dest = hitbox.Move(dx, 0);
        float newVX = dx;
        var oldTiles = new HashSet<(int, int)>(TilesForRect(hitbox));
        foreach (var (col, row) in TilesForRect(dest).Where(t => !oldTiles.Contains(t)))
        {
            Tile square;
            if (col < 0 || col >= Width || row < 0 || row >= Height)
                square = isPlayer ? new Tile(null, false, false, false, false)
                                  : new Tile(null, true, true, true, true);
            else
                square = _grid[col][row];

            var tileRect = RectForTile(col, row);

            if (hitbox.Right() < tileRect.Left() && square.SolidLeft && dest.Right() >= tileRect.Left())
                newVX = MathF.Min(newVX, tileRect.Left() - hitbox.Right() - 1);
            else if (hitbox.Left() > tileRect.Right() && square.SolidRight && dest.Left() <= tileRect.Right())
                newVX = MathF.Max(newVX, tileRect.Right() - hitbox.Left() + 1);
        }
        return hitbox.Move(newVX, 0);
    }

    /// <summary>
    /// Simplified collision check for projectiles — off-map is illegal.
    /// </summary>
    public bool IsMoveLegal(Rectangle hitbox, (float x, float y) vector)
    {
        var dest = hitbox.Move(vector.x, vector.y);
        var oldTiles = new HashSet<(int, int)>(TilesForRect(hitbox));
        foreach (var (col, row) in TilesForRect(dest).Where(t => !oldTiles.Contains(t)))
        {
            Tile square;
            if (col < 0 || col >= Width || row < 0 || row >= Height)
                square = new Tile(null, true, true, true, true);
            else
                square = _grid[col][row];

            var tileRect = RectForTile(col, row);

            if (hitbox.Bottom() < tileRect.Top() && square.SolidTop && dest.Bottom() >= tileRect.Top())
                return false;
            if (hitbox.Top() > tileRect.Bottom() && square.SolidBottom && dest.Top() <= tileRect.Bottom())
                return false;
            if (hitbox.Right() < tileRect.Left() && square.SolidLeft && dest.Right() >= tileRect.Left())
                return false;
            if (hitbox.Left() > tileRect.Right() && square.SolidRight && dest.Left() <= tileRect.Right())
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true if there is a solid tile in the direction of vector from rect.
    /// </summary>
    public bool IsRectSupported(Rectangle rect, (float x, float y) vector = default)
    {
        if (vector == default) vector = (0, 1);
        var dest = rect.Move(vector.x, vector.y);
	var oldTiles = new HashSet<(int, int)>(TilesForRect(rect));
        foreach (var (col, row) in TilesForRect(dest).Where(t => !oldTiles.Contains(t)))
        {
            if (col < 0 || col >= Width) continue;
            if (row < 0) return false;
            if (row >= Height) return true;
            if (_grid[col][row].SolidTop) return true;
        }
        return false;
    }

    public bool IsTileSupported(int col, int row) =>
        IsRectSupported(RectForTile(col, row));
}
