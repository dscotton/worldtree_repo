using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class GameStateTests : IDisposable
{
    // Shared test helpers

    private static Tile[][] EmptyGrid(int w, int h)
    {
        var grid = new Tile[w][];
        for (int col = 0; col < w; col++)
        {
            grid[col] = new Tile[h];
            for (int row = 0; row < h; row++)
                grid[col][row] = Tile.Empty;
        }
        return grid;
    }

    private class TestHero : Hero
    {
        public TestHero(Environment env, (int c, int r) p) : base(env, p) { }
        protected override void InitImages() { CurrentImage = new Texture2D { Width = 72, Height = 96 }; }
        protected override void SetCurrentImage() { Rect = Rect.WithSize(72, 96); }
    }

    // Reset shared static state after every test to prevent cross-test pollution.
    public void Dispose()
    {
        GameState.Reset();
        Environment.AllTransitions.Clear();
    }

    private static void SetupTransition(
        string srcRoom, int srcRegion, TransitionDirection dir,
        string destRoom, int destRegion,
        int first, int last, int offset = 0)
    {
        Environment.AllTransitions.TryAdd(srcRegion, new());
        Environment.AllTransitions[srcRegion].TryAdd(srcRoom, new());
        Environment.AllTransitions[srcRegion][srcRoom].TryAdd(dir, new());
        Environment.AllTransitions[srcRegion][srcRoom][dir].Add(new TransitionInfo
        {
            First = first, Last = last,
            Region = destRegion, Dest = destRoom,
            Offset = offset
        });
    }

    // --- Tests ---

    [Fact]
    public void HandleRoomTransition_RightExit_UpdatesCurrentRoom()
    {
        // Source: 30×20 tiles (1440×960 px). Dest: 20×15 tiles.
        // Transition: row range 5–15, zero offset.
        var srcEnv  = new Environment(30, 20, EmptyGrid(30, 20));
        var destEnv = new Environment(20, 15, EmptyGrid(20, 15));
        SetupTransition("TestMap", 1, TransitionDirection.Right, "DestRoom", 1, first: 5, last: 15);
        GameState.Reset("TestMap", 1);
        GameState.RoomCache[(1, "DestRoom")] = destEnv;

        // Position hero past the right edge (X > 30*48=1440), hitbox top at tile row 10.
        var hero = new TestHero(srcEnv, (0, 0));
        hero.Rect = new Rectangle(30 * 48 + 10f, 10 * 48f, 72, 96);
        var camera = new Camera2D();

        GameState.HandleRoomTransition(ref srcEnv, ref hero, ref camera);

        Assert.Equal("DestRoom", GameState.CurrentRoom);
    }

    [Fact]
    public void HandleRoomTransition_RightExit_SavesSourceToCache()
    {
        var srcEnv  = new Environment(30, 20, EmptyGrid(30, 20));
        var destEnv = new Environment(20, 15, EmptyGrid(20, 15));
        SetupTransition("TestMap", 1, TransitionDirection.Right, "DestRoom", 1, first: 5, last: 15);
        GameState.Reset("TestMap", 1);
        GameState.RoomCache[(1, "DestRoom")] = destEnv;

        var hero = new TestHero(srcEnv, (0, 0));
        hero.Rect = new Rectangle(30 * 48 + 10f, 10 * 48f, 72, 96);
        var camera = new Camera2D();

        GameState.HandleRoomTransition(ref srcEnv, ref hero, ref camera);

        Assert.True(GameState.RoomCache.ContainsKey((1, "TestMap")));
    }

    [Fact]
    public void HandleRoomTransition_RightExit_EnvBecomesDestination()
    {
        var srcEnv  = new Environment(30, 20, EmptyGrid(30, 20));
        var destEnv = new Environment(20, 15, EmptyGrid(20, 15));
        SetupTransition("TestMap", 1, TransitionDirection.Right, "DestRoom", 1, first: 5, last: 15);
        GameState.Reset("TestMap", 1);
        GameState.RoomCache[(1, "DestRoom")] = destEnv;

        var hero = new TestHero(srcEnv, (0, 0));
        hero.Rect = new Rectangle(30 * 48 + 10f, 10 * 48f, 72, 96);
        var camera = new Camera2D();

        GameState.HandleRoomTransition(ref srcEnv, ref hero, ref camera);

        // srcEnv is passed by ref; after transition it should be the dest env.
        Assert.Same(destEnv, srcEnv);
    }

    [Fact]
    public void HandleRoomTransition_DownExit_UpdatesCurrentRoom()
    {
        // Source: 20×15 tiles (960×720 px). Dest: 20×10 tiles.
        // Transition: col range 3–12, zero offset.
        var srcEnv  = new Environment(20, 15, EmptyGrid(20, 15));
        var destEnv = new Environment(20, 10, EmptyGrid(20, 10));
        SetupTransition("TestMap", 1, TransitionDirection.Down, "DestRoom", 1, first: 3, last: 12);
        GameState.Reset("TestMap", 1);
        GameState.RoomCache[(1, "DestRoom")] = destEnv;

        // Position hero past the bottom edge (Y > 15*48=720), hitbox left at tile col 8.
        var hero = new TestHero(srcEnv, (0, 0));
        hero.Rect = new Rectangle(8 * 48f, 15 * 48 + 10f, 72, 96);
        var camera = new Camera2D();

        GameState.HandleRoomTransition(ref srcEnv, ref hero, ref camera);

        Assert.Equal("DestRoom", GameState.CurrentRoom);
    }
}
