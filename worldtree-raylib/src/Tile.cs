// src/Tile.cs
using Raylib_cs;

namespace WorldTree;

/// <summary>
/// A single map tile. Tiles have no notion of position; the Environment places them.
/// Corresponds to worldtree/tile.py.
/// </summary>
public class Tile
{
    public static readonly Tile Empty = new Tile(null, false, false, false, false);

    public Texture2D? Image { get; }
    public bool SolidLeft   { get; }
    public bool SolidRight  { get; }
    public bool SolidTop    { get; }
    public bool SolidBottom { get; }

    public bool IsEmpty => Image == null && !SolidLeft && !SolidRight && !SolidTop && !SolidBottom;

    public Tile(Texture2D? image, bool solidLeft, bool solidRight, bool solidTop, bool solidBottom)
    {
        Image = image;
        SolidLeft = solidLeft;
        SolidRight = solidRight;
        SolidTop = solidTop;
        SolidBottom = solidBottom;
    }

    /// <summary>
    /// Parse a TileStudio format bound byte into a Tile (without image).
    /// Bit layout: bit0=upper(top), bit1=left, bit2=lower(bottom), bit3=right.
    /// </summary>
    public static Tile FromBoundByte(byte bound)
    {
        bool solidLeft   = (bound & 2) != 0;
        bool solidRight  = (bound & 8) != 0;
        bool solidTop    = (bound & 1) != 0;
        bool solidBottom = (bound & 4) != 0;
        return new Tile(null, solidLeft, solidRight, solidTop, solidBottom);
    }

    public static Tile WithImage(Texture2D image, byte boundByte)
    {
        var proto = FromBoundByte(boundByte);
        return new Tile(image, proto.SolidLeft, proto.SolidRight, proto.SolidTop, proto.SolidBottom);
    }
}
