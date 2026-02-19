using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class CollisionTests
{
    private Environment CreateTestEnvironment(int width, int height, Tile[][] grid)
    {
        return new Environment(width, height, grid);
    }

    [Fact]
    public void TilesForRect_ReturnsCorrectIndices()
    {
        // Setup: 3x3 grid
        var grid = new Tile[3][];
        for (int i = 0; i < 3; i++) grid[i] = new Tile[3];
        var env = CreateTestEnvironment(3, 3, grid);

        // Rect covering (0,0) and part of (1,0)
        // TileWidth=48, TileHeight=48
        // Rect(10, 10, 50, 20) -> spans x=[10..60], y=[10..30]
        // x=10 is col 0, x=60 is col 1. y=10..30 is row 0.
        var rect = new Rectangle(10, 10, 50, 20);
        
        var tiles = env.TilesForRect(rect);
        
        Assert.Contains((0, 0), tiles);
        Assert.Contains((1, 0), tiles);
        Assert.DoesNotContain((0, 1), tiles);
        Assert.Equal(2, tiles.Count);
    }

    [Fact]
    public void IsMoveLegal_BlocksSolid()
    {
        // Grid:
        // [Empty][Solid]
        // [Empty][Empty]
        var grid = new Tile[2][];
        grid[0] = new Tile[] { Tile.Empty, Tile.Empty };
        grid[1] = new Tile[] { 
            new Tile(null, true, true, true, true), // Solid block at (1,0)
            Tile.Empty 
        };
        var env = CreateTestEnvironment(2, 2, grid);

        var playerRect = new Rectangle(10, 10, 20, 20); // entirely in (0,0)

        // Move right into (1,0) -> Should be blocked
        // (10,10) + (50,0) -> (60,10) which is inside (1,0)
        Assert.False(env.IsMoveLegal(playerRect, (50, 0)));

        // Move down into (0,1) -> Should be OK
        Assert.True(env.IsMoveLegal(playerRect, (0, 50)));
    }

    [Fact]
    public void AttemptMove_CorrectResolution()
    {
        // Grid:
        // [Empty]
        // [SolidTop]
        var grid = new Tile[1][];
        // (0,0) Empty, (0,1) SolidTop
        grid[0] = new Tile[] { 
            Tile.Empty, 
            new Tile(null, false, false, true, false) 
        };
        var env = CreateTestEnvironment(1, 2, grid);

        // Player falling onto floor
        // Player at (10, 10) (in 0,0), 20x20 size. Bottom at 30.
        // Floor at row 1 starts at y=48.
        // Move (0, 30) -> Target y=40, Bottom=60. 
        // Should stop at y=48 - 20 - 1 = 27? Wait, let's check logic.
        // Logic: if (hitbox.Bottom() < tileRect.Top() && square.SolidTop && dest.Bottom() >= tileRect.Top())
        // newVY = tileRect.Top() - hitbox.Bottom() - 1;
        
        var rect = new Rectangle(10, 10, 20, 20); // Bottom is 30.
        var move = (0f, 30f); // Target y=40, Bottom=60.
        
        // Tile (0,1) Top is 48.
        // 30 < 48 AND 60 >= 48 -> Collision!
        // newVY = 48 - 30 - 1 = 17.
        // Final Y = 10 + 17 = 27. Bottom = 47.
        // Wait, logic says newVY = tileRect.Top() - hitbox.Bottom() - 1?
        // Let's re-read logic from plan carefully.
        
        // "newVY = tileRect.Top() - hitbox.Bottom() - 1"
        // If y=10, h=20, bottom=30.
        // Tile top=48.
        // 48 - 30 - 1 = 17.
        // New Y = 10 + 17 = 27.
        // New Bottom = 47.
        // 47 < 48. Correct, just above floor.

        var result = env.AttemptMove(rect, move);
        
        Assert.Equal(10, result.X); // X shouldn't change
        Assert.Equal(27, result.Y); // Y adjusted
    }

    [Fact]
    public void IsRectSupported_DetectsGround()
    {
        // Grid:
        // [Empty]
        // [SolidTop]
        var grid = new Tile[1][];
        grid[0] = new Tile[] { 
            Tile.Empty, 
            new Tile(null, false, false, true, false) 
        };
        var env = CreateTestEnvironment(1, 2, grid);

        // Rect just above floor (y=27, h=20, bottom=47). Floor at 48.
        // Check (0,1) vector support.
        // Dest rect y=28, bottom=48.
        // Overlap with (0,1)? Yes.
        // (0,1) is SolidTop? Yes.
        // Should return True.

        var rect = new Rectangle(10, 27, 20, 20);
        Assert.True(env.IsRectSupported(rect, (0, 1)));

        // Rect high up (y=0)
        var airRect = new Rectangle(10, 0, 20, 20);
        Assert.False(env.IsRectSupported(airRect, (0, 1)));
    }
}
