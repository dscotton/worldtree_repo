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

    // --- New tests ---

    // Tile helpers  (TileWidth = TileHeight = 48)
    private static Tile SolidLeft()   => new Tile(null, true,  false, false, false);
    private static Tile SolidRight()  => new Tile(null, false, true,  false, false);
    private static Tile SolidTop()    => new Tile(null, false, false, true,  false);
    private static Tile SolidBottom() => new Tile(null, false, false, false, true);
    private static Tile AllSolid()    => new Tile(null, true,  true,  true,  true);

    [Fact]
    public void AttemptMove_HorizontalRight_BlockedBySolidLeftWall()
    {
        // Grid (col x row):  [Empty(0,0)] [SolidLeft(1,0)]
        var grid = new Tile[2][];
        grid[0] = new[] { Tile.Empty };
        grid[1] = new[] { SolidLeft() };
        var env = CreateTestEnvironment(2, 1, grid);

        // Rect at (10,10) size 20x20.  Right edge = 30.  Wall left edge = 48.
        // Move +30: dest right = 60 >= 48, hitbox right 30 < 48 → blocked.
        // newVX = 48 - 30 - 1 = 17  →  X = 27, right = 47 (just clear of wall).
        var result = env.AttemptMove(new Rectangle(10, 10, 20, 20), (30, 0));
        Assert.Equal(27, result.X);
        Assert.Equal(10, result.Y);
    }

    [Fact]
    public void AttemptMove_HorizontalLeft_BlockedBySolidRightWall()
    {
        // Grid: [SolidRight(0,0)] [Empty(1,0)]
        var grid = new Tile[2][];
        grid[0] = new[] { SolidRight() };
        grid[1] = new[] { Tile.Empty };
        var env = CreateTestEnvironment(2, 1, grid);

        // Rect at (58,10) size 20x20.  Left = 58.  Wall right edge = 47.
        // Move -30: dest left = 28 <= 47, hitbox left 58 > 47 → blocked.
        // newVX = 47 - 58 + 1 = -10  →  X = 48, left = 48 (just clear of wall).
        var result = env.AttemptMove(new Rectangle(58, 10, 20, 20), (-30, 0));
        Assert.Equal(48, result.X);
        Assert.Equal(10, result.Y);
    }

    [Fact]
    public void AttemptMove_VerticalUp_BlockedBySolidBottomCeiling()
    {
        // Grid: [SolidBottom(0,0)]
        //       [Empty(0,1)]
        var grid = new Tile[1][];
        grid[0] = new[] { SolidBottom(), Tile.Empty };
        var env = CreateTestEnvironment(1, 2, grid);

        // Rect at (10,60) size 20x20. Top = 60.  Ceiling bottom edge = 47.
        // Move -30: dest top = 30 <= 47, hitbox top 60 > 47 → blocked.
        // newVY = 47 - 60 + 1 = -12  →  Y = 48, top = 48 (just clear of ceiling).
        var result = env.AttemptMove(new Rectangle(10, 60, 20, 20), (0, -30));
        Assert.Equal(10, result.X);
        Assert.Equal(48, result.Y);
    }

    [Fact]
    public void AttemptMove_YFirstResolution_MovingUpAndRight_ClearsWallAtOriginalRow()
    {
        // Grid:  [Empty(0,0)]    [Empty(1,0)]     ← no wall at destination row
        //        [Empty(0,1)]    [SolidLeft(1,1)] ← wall only at starting row
        //        [Empty(0,2)]    [Empty(1,2)]
        var grid = new Tile[2][];
        grid[0] = new[] { Tile.Empty, Tile.Empty, Tile.Empty };
        grid[1] = new[] { Tile.Empty, SolidLeft(), Tile.Empty };
        var env = CreateTestEnvironment(2, 3, grid);

        // Rect at (10,60) size 20x20: entirely in row 1 (60/48=1, 80/48=1).
        // Move (+30, -40):
        //   Y-first: moves up to row 0 (y=20), then MoveX sees no wall in row 0 → passes.
        //   X-first: would hit SolidLeft in row 1 and be blocked at X=27.
        var result = env.AttemptMove(new Rectangle(10, 60, 20, 20), (30, -40));
        Assert.Equal(40, result.X); // cleared the wall
        Assert.Equal(20, result.Y);
    }

    [Fact]
    public void AttemptMove_LandsOnNearestFloorWhenMultipleInPath()
    {
        // Grid: [Empty(0,0)]
        //       [SolidTop(0,1)]   ← nearer floor
        //       [SolidTop(0,2)]   ← farther floor — should NOT be chosen
        var grid = new Tile[1][];
        grid[0] = new[] { Tile.Empty, SolidTop(), SolidTop() };
        var env = CreateTestEnvironment(1, 3, grid);

        // Rect at (5,10) size 20x20.  Bottom = 30.  Move down 80px.
        // Dest bottom = 110, spanning rows 1 and 2.
        // Floor at row 1 top = 48: newVY = min(80, 48-30-1) = 17 → Y = 27, bottom = 47.
        // Floor at row 2 top = 96: newVY = min(17, 96-30-1) = 17 (unchanged).
        // Without MathF.Min (old "last-write-wins"), row 2 would overwrite: Y = 75.
        var result = env.AttemptMove(new Rectangle(5, 10, 20, 20), (0, 80));
        Assert.Equal(5,  result.X);
        Assert.Equal(27, result.Y); // landed on nearer floor, not the farther one
    }

    [Fact]
    public void AttemptMove_PlayerCanExitMapEdge()
    {
        // 2-col grid, player moves far off the right edge.
        var grid = new Tile[2][];
        grid[0] = new[] { Tile.Empty };
        grid[1] = new[] { Tile.Empty };
        var env = CreateTestEnvironment(2, 1, grid);

        // isPlayer=true: out-of-bounds tiles treated as non-solid → movement allowed.
        var result = env.AttemptMove(new Rectangle(10, 10, 20, 20), (200, 0), isPlayer: true);
        Assert.Equal(210, result.X);
    }

    [Fact]
    public void AttemptMove_EnemyBlockedAtMapEdge()
    {
        // Same 2-col grid, enemy moves toward right edge.
        var grid = new Tile[2][];
        grid[0] = new[] { Tile.Empty };
        grid[1] = new[] { Tile.Empty };
        var env = CreateTestEnvironment(2, 1, grid);

        // Enemy at (70,10) size 20x20. Right=90. Map right boundary at col 2 → x=96.
        // Move +10: dest right=100 >= 96 → blocked. newVX = 96-90-1 = 5 → X = 75, right = 95.
        var result = env.AttemptMove(new Rectangle(70, 10, 20, 20), (10, 0), isPlayer: false);
        Assert.Equal(75, result.X);
    }

    [Fact]
    public void IsRectSupported_WallBesideCharacterDoesNotCountAsSupport()
    {
        // Grid: [Empty(0,0)]        [SolidLeft+SolidTop(1,0)]
        //       [Empty(0,1)]        [Empty(1,1)]
        // A wall tile sits to the right of the player; there is no floor below.
        // The shifted fallbox overlaps the wall column, but the wall's SolidTop
        // should NOT trigger support — those tiles are "old tiles" for the probe.
        var grid = new Tile[2][];
        grid[0] = new[] { Tile.Empty, Tile.Empty };
        grid[1] = new[] { new Tile(null, true, false, true, false), Tile.Empty };
        var env = CreateTestEnvironment(2, 2, grid);

        // Shift rect into the wall column: (40,10,20,20) spans cols 0 and 1.
        // Probe 1px down: bottom goes 30→31, still row 0 — no NEW tiles enter.
        // Old buggy code (no filter) would check (1,0).SolidTop and return true.
        var shiftedRect = new Rectangle(40, 10, 20, 20);
        Assert.False(env.IsRectSupported(shiftedRect));
    }
}
