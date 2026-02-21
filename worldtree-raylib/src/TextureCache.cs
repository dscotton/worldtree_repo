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
    /// </summary>
    public static Texture2D LoadImage(string filename, bool scaled = false)
    {
        string path = Path.Combine(GameConstants.SpritesDir, filename);
        string key = $"{path}|{scaled}";
        if (_cache.TryGetValue(key, out var cached)) return cached;
        var tex = LoadFromPath(path, scaled);
        _cache[key] = tex;
        return tex;
    }

    /// <summary>
    /// Load all images matching a glob pattern (e.g. "beaver1*.png"), sorted alphabetically.
    /// </summary>
    public static Texture2D[] LoadImages(string pattern, bool scaled = false)
    {
        string[] files = Directory.GetFiles(GameConstants.SpritesDir, pattern)
                                  .OrderBy(f => f)
                                  .ToArray();
        return files.Select(f =>
        {
            string key = $"{f}|{scaled}";
            if (_cache.TryGetValue(key, out var cached)) return cached;
            var tex = LoadFromPath(f, scaled);
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

    private static Texture2D LoadFromPath(string path, bool scaled)
    {
        Image img = Raylib.LoadImage(path);
        
        // Ensure image has an alpha channel so transparency works
        Raylib.ImageFormat(ref img, PixelFormat.UncompressedR8G8B8A8);

        if (scaled)
            Raylib.ImageResizeNN(ref img, img.Width * 3, img.Height * 3); // Use NN for pixel art crispness

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return tex;
    }
}
