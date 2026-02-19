// src/TextureCache.cs
using Raylib_cs;

namespace WorldTree;

/// <summary>
/// Loads and caches Texture2D objects. Requires Raylib to be initialized.
/// Corresponds to the LoadImage/LoadImages helpers in worldtree/characters/character.py.
/// </summary>
public static class TextureCache
{
    private static readonly Dictionary<string, Texture2D> _cache = new();

    /// <summary>
    /// Load a single image. 'scaled' means 3x upscale (for sprites).
    /// 'colorkey' means replace #FF00FF with transparency.
    /// </summary>
    public static Texture2D LoadImage(string filename, bool scaled = false, bool colorkey = false)
    {
        string path = Path.Combine(GameConstants.SpritesDir, filename);
        string key = $"{path}|{scaled}|{colorkey}";
        if (_cache.TryGetValue(key, out var cached)) return cached;
        var tex = LoadFromPath(path, scaled, colorkey);
        _cache[key] = tex;
        return tex;
    }

    /// <summary>
    /// Load all images matching a glob pattern (e.g. "beaver1*.png"), sorted alphabetically.
    /// </summary>
    public static Texture2D[] LoadImages(string pattern, bool scaled = false, bool colorkey = false)
    {
        string[] files = Directory.GetFiles(GameConstants.SpritesDir, pattern)
                                  .OrderBy(f => f)
                                  .ToArray();
        return files.Select(f =>
        {
            string key = $"{f}|{scaled}|{colorkey}";
            if (_cache.TryGetValue(key, out var cached)) return cached;
            var tex = LoadFromPath(f, scaled, colorkey);
            _cache[key] = tex;
            return tex;
        }).ToArray();
    }

    /// <summary>Return a horizontally-flipped copy of a texture (for left/right variants).</summary>
    public static Texture2D FlipHorizontal(Texture2D source)
    {
        Image img = Raylib.LoadImageFromTexture(source);
        Raylib.ImageFlipHorizontal(ref img);
        Texture2D flipped = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return flipped;
    }

    /// <summary>Load a tile image scaled to TileWidth x TileHeight.</summary>
    public static Texture2D LoadTile(string path)
    {
        string key = $"tile|{path}";
        if (_cache.TryGetValue(key, out var cached)) return cached;
        Image img = Raylib.LoadImage(path);
        Raylib.ImageResize(ref img, GameConstants.TileWidth, GameConstants.TileHeight);
        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        _cache[key] = tex;
        return tex;
    }

    public static void UnloadAll()
    {
        foreach (var tex in _cache.Values)
            Raylib.UnloadTexture(tex);
        _cache.Clear();
    }

    private static Texture2D LoadFromPath(string path, bool scaled, bool colorkey)
    {
        Image img = Raylib.LoadImage(path);
        if (colorkey)
            Raylib.ImageColorReplace(ref img, GameConstants.SpriteColorkey, Color.Blank);
        if (scaled)
            Raylib.ImageResize(ref img, img.Width * 3, img.Height * 3);
        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return tex;
    }
}
