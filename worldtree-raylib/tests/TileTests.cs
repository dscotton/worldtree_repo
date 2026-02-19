// tests/TileTests.cs
using WorldTree;

namespace WorldTree.Tests;

public class TileTests
{
    // ParseBoundByte bit layout (TileStudio format):
    // bit 0 (value 1) = upper (solid_top)
    // bit 1 (value 2) = left  (solid_left)
    // bit 2 (value 4) = lower (solid_bottom)
    // bit 3 (value 8) = right (solid_right)
    [Theory]
    [InlineData(0,  false, false, false, false)]
    [InlineData(15, true,  true,  true,  true)]
    [InlineData(1,  false, false, true,  false)]  // bit 0 = top
    [InlineData(2,  true,  false, false, false)]  // bit 1 = left
    [InlineData(4,  false, false, false, true)]   // bit 2 = bottom... wait, check python
    [InlineData(8,  false, true,  false, false)]  // bit 3 = right
    public void ParseBoundByte_CorrectSolidity(
        byte bound, bool left, bool right, bool top, bool bottom)
    {
        var tile = Tile.FromBoundByte(bound);
        Assert.Equal(left,   tile.SolidLeft);
        Assert.Equal(right,  tile.SolidRight);
        Assert.Equal(top,    tile.SolidTop);
        Assert.Equal(bottom, tile.SolidBottom);
    }

    [Fact]
    public void EmptyTile_NotSolid()
    {
        var tile = Tile.Empty;
        Assert.False(tile.SolidLeft);
        Assert.False(tile.SolidRight);
        Assert.False(tile.SolidTop);
        Assert.False(tile.SolidBottom);
    }
}
