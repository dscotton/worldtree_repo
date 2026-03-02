# Test Coverage Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `SetScreenOffset` edge-case tests and a `GameState` static class with a testable `HandleRoomTransition` method.

**Architecture:** The existing `enum GameState` (Playing/Paused/GameOver/Won) is renamed to `PlayState` to free the name for a new `GameState` static class that holds session-level data (room cache, visited rooms, current room/region). `HandleRoomTransition` is promoted from a local function in `RunGame()` to a static method on `GameState`, taking only `ref env`, `ref player`, and `ref camera` — all other data it needs is accessed directly via `GameState.*` or `Environment.AllTransitions`.

**Tech Stack:** C# 13, xUnit 2.9.2, Raylib-cs (tests avoid Raylib calls by using the `internal Environment(int, int, Tile[][])` test constructor and a `TestHero` subclass that no-ops `InitImages`).

**Key constants (needed for expected values):**
- `GameConstants.MapWidth = 1280`, `MapHeight = 640`, `TileWidth = TileHeight = 48`

---

### Task 1: Fix existing `SetScreenOffset_ClampsToBounds` test

The `ScreenWidth` changed from 960 to 1280, so the expected clamp for X is now `2400 - 1280 = 1120`, not `1440`.

**Files:**
- Modify: `worldtree-raylib/tests/EnvironmentTests.cs:38-44`

**Step 1: Update expected values**

Replace the current assertion and comment block:

```csharp
// Max offset = (2400 - 960, 1920 - 720) = (1440, 1200)
// Note: GameConstants.MapHeight is 640, MapWidth is 960.
// ScreenHeight is 720, but MapY is 80. Viewport is 960x640.
// Max offset = (2400 - 960, 1920 - 640) = (1440, 1280)

env.SetScreenOffset(2000, 2000);
Assert.Equal(1440, env.ScreenOffset.X);
Assert.Equal(1280, env.ScreenOffset.Y);
```

With:

```csharp
// Room is 50×40 tiles = 2400×1920 px.
// maxX = 2400 - 1280 = 1120, maxY = 1920 - 640 = 1280
env.SetScreenOffset(2000, 2000);
Assert.Equal(1120, env.ScreenOffset.X);
Assert.Equal(1280, env.ScreenOffset.Y);
```

**Step 2: Run tests**

```bash
cd worldtree-raylib && dotnet test --filter "EnvironmentTests" -v normal
```

Expected: all `EnvironmentTests` pass.

**Step 3: Commit**

```bash
git add worldtree-raylib/tests/EnvironmentTests.cs
git commit -m "fix: update SetScreenOffset test expected value for new MapWidth=1280"
```

---

### Task 2: Add `SetScreenOffset` edge-case tests

These test the centering behavior for rooms smaller than the viewport.

**Files:**
- Modify: `worldtree-raylib/tests/EnvironmentTests.cs`

**Step 1: Add four tests** inside the `EnvironmentTests` class, after the existing `SetScreenOffset_ClampsToBounds` test:

```csharp
[Fact]
public void SetScreenOffset_NarrowRoom_CentersX()
{
    // 10×20 tiles = 480×960 px. Room narrower than MapWidth (1280).
    // maxX = 480 - 1280 = -800 → center = -400. Y is normal: max = 960-640 = 320.
    SetupMockRegion(10, 20);
    var env = new Environment("TestMap", 1);

    env.SetScreenOffset(0, 0);

    Assert.Equal(-400f, env.ScreenOffset.X);
    Assert.Equal(0f,    env.ScreenOffset.Y);
}

[Fact]
public void SetScreenOffset_ShortRoom_CentersY()
{
    // 30×10 tiles = 1440×480 px. Room shorter than MapHeight (640).
    // maxY = 480 - 640 = -160 → center = -80. X is normal: max = 1440-1280 = 160.
    SetupMockRegion(30, 10);
    var env = new Environment("TestMap", 1);

    env.SetScreenOffset(0, 0);

    Assert.Equal(0f,   env.ScreenOffset.X);
    Assert.Equal(-80f, env.ScreenOffset.Y);
}

[Fact]
public void SetScreenOffset_NarrowAndShortRoom_CentersBothAxes()
{
    // 10×10 tiles = 480×480 px. Smaller than screen on both axes.
    SetupMockRegion(10, 10);
    var env = new Environment("TestMap", 1);

    env.SetScreenOffset(0, 0);

    Assert.Equal(-400f, env.ScreenOffset.X);
    Assert.Equal(-80f,  env.ScreenOffset.Y);
}

[Fact]
public void SetScreenOffset_LargeRoom_ClampsNormally()
{
    // 28×16 tiles = 1344×768 px. Larger than screen on both axes.
    // maxX = 1344-1280 = 64, maxY = 768-640 = 128.
    SetupMockRegion(28, 16);
    var env = new Environment("TestMap", 1);

    env.SetScreenOffset(200, 200);

    Assert.Equal(64f,  env.ScreenOffset.X);
    Assert.Equal(128f, env.ScreenOffset.Y);
}
```

**Step 2: Run tests**

```bash
dotnet test --filter "EnvironmentTests" -v normal
```

Expected: all 7 `EnvironmentTests` pass (3 existing + 4 new).

**Step 3: Commit**

```bash
git add worldtree-raylib/tests/EnvironmentTests.cs
git commit -m "test: add SetScreenOffset edge cases for narrow and short rooms"
```

---

### Task 3: Rename `enum GameState` → `enum PlayState`

The name `GameState` is needed for our new static class. The existing enum (Playing/Paused/GameOver/Won) is renamed to `PlayState`.

**Files:**
- Modify: `worldtree-raylib/src/GameConstants.cs`
- Modify: `worldtree-raylib/src/Program.cs`

**Step 1: Rename the enum in `GameConstants.cs`**

Find:
```csharp
public enum GameState { Playing, Paused, GameOver, Won }
```

Replace with:
```csharp
public enum PlayState { Playing, Paused, GameOver, Won }
```

**Step 2: Update all references in `Program.cs`**

Use find-replace (exact strings) to update the following — there are roughly 10 occurrences:

| Find | Replace |
|------|---------|
| `var gameState = GameState.Playing;` | `var playState = PlayState.Playing;` |
| `gameState is GameState.Playing or GameState.Paused or GameState.GameOver` | `playState is PlayState.Playing or PlayState.Paused or PlayState.GameOver` |
| `if (gameState == GameState.GameOver)` | `if (playState == PlayState.GameOver)` |
| `if (gameState == GameState.Paused)` | `if (playState == PlayState.Paused)` |
| `if (gameState == GameState.Playing)` | `if (playState == PlayState.Playing)` |
| `gameState = GameState.Playing` | `playState = PlayState.Playing` |
| `gameState = GameState.Paused` | `playState = PlayState.Paused` |
| `gameState = GameState.GameOver` | `playState = PlayState.GameOver` |
| `gameState is GameState.Won` | `playState is PlayState.Won` |
| `gameState ==` | `playState ==` (catch any remaining) |

**Step 3: Build to verify no compile errors**

```bash
dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

**Step 4: Run all tests**

```bash
dotnet test -v normal
```

Expected: all tests pass.

**Step 5: Commit**

```bash
git add worldtree-raylib/src/GameConstants.cs worldtree-raylib/src/Program.cs
git commit -m "refactor: rename enum GameState -> PlayState to free name for GameState class"
```

---

### Task 4: Write failing `GameStateTests.cs`

Write tests before `GameState` exists so we can confirm they fail correctly first.

**Files:**
- Create: `worldtree-raylib/tests/GameStateTests.cs`

**Step 1: Create the test file**

```csharp
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
```

**Step 2: Run to verify they fail**

```bash
dotnet test --filter "GameStateTests" -v normal
```

Expected: all 4 tests FAIL with a compile error or `CS0103: The name 'GameState' does not exist`.

**Step 3: Commit the failing tests**

```bash
git add worldtree-raylib/tests/GameStateTests.cs
git commit -m "test: add failing GameState/HandleRoomTransition tests"
```

---

### Task 5: Create `GameState.cs`

**Files:**
- Create: `worldtree-raylib/src/GameState.cs`

**Step 1: Create the file**

```csharp
// src/GameState.cs
using Raylib_cs;
using System.Numerics;

namespace WorldTree;

/// <summary>
/// Holds session-level game state (one instance per play-through).
/// Static because there is exactly one game session at a time.
/// </summary>
public static class GameState
{
    public const int RoomCacheSize = 6;

    public static string CurrentRoom   { get; set; } = "Map1";
    public static int    CurrentRegion { get; set; } = 1;

    public static Dictionary<int, HashSet<string>>                  VisitedRooms { get; set; } = new();
    public static Dictionary<(int region, string room), Environment> RoomCache   { get; set; } = new();
    public static Queue<(int region, string room)>                   CacheQueue  { get; set; } = new();

    /// <summary>Initialise (or re-initialise) all session state. Call at the start of RunGame and in test teardown.</summary>
    public static void Reset(string startRoom = "Map1", int startRegion = 1)
    {
        CurrentRoom   = startRoom;
        CurrentRegion = startRegion;
        VisitedRooms  = new Dictionary<int, HashSet<string>>
            { [startRegion] = new HashSet<string> { startRoom } };
        RoomCache  = new Dictionary<(int, string), Environment>();
        CacheQueue = new Queue<(int, string)>();
    }

    /// <summary>
    /// Resolve a room transition triggered when the player's fallbox exits the map.
    /// Updates CurrentRoom, CurrentRegion, VisitedRooms, RoomCache, env, player, and camera.
    /// Music update is NOT performed here — call TryStartMusic in Program.cs after this returns.
    /// </summary>
    public static void HandleRoomTransition(
        ref Environment env, ref Hero player, ref Camera2D camera)
    {
        // Determine exit direction from fallbox centre position.
        TransitionDirection dir;
        Rectangle bounds = player.Fallbox();
        if      (bounds.CenterX() < 0)                                  dir = TransitionDirection.Left;
        else if (bounds.CenterX() > env.Width * GameConstants.TileWidth) dir = TransitionDirection.Right;
        else if (bounds.CenterY() < 0)                                  dir = TransitionDirection.Up;
        else                                                              dir = TransitionDirection.Down;

        if (!Environment.AllTransitions.TryGetValue(CurrentRegion, out var regionTrans)) return;
        if (!regionTrans.TryGetValue(CurrentRoom, out var roomTrans))                    return;
        if (!roomTrans.TryGetValue(dir, out var transList))                              return;

        // Match transition by the upper-left tile of the player's hitbox.
        var ulTile = env.TileIndexForPoint(player.Hitbox().Left(), player.Hitbox().Top());
        int ulCol  = ulTile.col;
        int ulRow  = ulTile.row;

        TransitionInfo? match = null;
        foreach (var t in transList)
        {
            bool inRange = (dir == TransitionDirection.Left || dir == TransitionDirection.Right)
                ? (ulRow >= t.First && ulRow <= t.Last)
                : (ulCol >= t.First && ulCol <= t.Last);
            if (inRange) { match = t; break; }
        }
        if (match == null) return;

        // Save outgoing room to the LRU cache.
        var exitKey = (CurrentRegion, CurrentRoom);
        if (!RoomCache.ContainsKey(exitKey))
        {
            CacheQueue.Enqueue(exitKey);
            while (CacheQueue.Count > RoomCacheSize)
                RoomCache.Remove(CacheQueue.Dequeue());
        }
        RoomCache[exitKey] = env;

        // Update current location.
        CurrentRegion = match.Region;
        CurrentRoom   = match.Dest;
        VisitedRooms.TryAdd(CurrentRegion, new HashSet<string>());
        VisitedRooms[CurrentRegion].Add(CurrentRoom);

        // Restore cached destination or load fresh.
        if (!RoomCache.TryGetValue((CurrentRegion, CurrentRoom), out var nextEnv))
            nextEnv = new Environment(CurrentRoom, CurrentRegion);
        env = nextEnv;

        // Place player at the entry tile.
        int newCol = 0, newRow = 0;
        if      (dir == TransitionDirection.Left)  { newCol = env.Width  - 1; newRow = ulRow + match.Offset; }
        else if (dir == TransitionDirection.Right) { newCol = 0;              newRow = ulRow + match.Offset; }
        else if (dir == TransitionDirection.Up)    { newCol = ulCol + match.Offset; newRow = env.Height - 1; }
        else if (dir == TransitionDirection.Down)  { newCol = ulCol + match.Offset; newRow = 0;              }

        player.ChangeRooms(env, (newCol, newRow));

        // Reset camera to new room.
        env.SetScreenOffset(player.Rect.CenterX() - GameConstants.ScreenWidth  / 2f,
                            player.Rect.CenterY() - GameConstants.ScreenHeight / 2f);
        camera = env.MakeCamera();
        camera = env.Scroll(camera, player.Fallbox());
    }
}
```

**Step 2: Run the tests**

```bash
dotnet test --filter "GameStateTests" -v normal
```

Expected: all 4 `GameStateTests` pass.

**Step 3: Run the full test suite to check nothing broke**

```bash
dotnet test -v normal
```

Expected: all tests pass.

**Step 4: Commit**

```bash
git add worldtree-raylib/src/GameState.cs
git commit -m "feat: add GameState static class with HandleRoomTransition"
```

---

### Task 6: Update `Program.cs` to use `GameState`

**Files:**
- Modify: `worldtree-raylib/src/Program.cs`

**Step 1: Replace session-local variables with `GameState.Reset()`**

In `RunGame()`, find and remove these four locals (they are now in `GameState`):

```csharp
string currentRoom = "Map1";
int currentRegion = 1;
// ...
var visitedRooms = new Dictionary<int, HashSet<string>>
    { [currentRegion] = new HashSet<string> { currentRoom } };
var roomCache  = new Dictionary<(int region, string room), Env>();
var cacheQueue = new Queue<(int region, string room)>();
```

Replace with a single call **before** the env/player/camera setup lines:

```csharp
GameState.Reset();
```

Also remove the top-level constant (near line 29):

```csharp
const int RoomCacheSize = 6;
```

(It is now `GameState.RoomCacheSize`.)

**Step 2: Update references to the removed locals**

Search `Program.cs` for `currentRoom` and `currentRegion` and replace:

| Old | New |
|-----|-----|
| `currentRoom` | `GameState.CurrentRoom` |
| `currentRegion` | `GameState.CurrentRegion` |
| `visitedRooms` | `GameState.VisitedRooms` |

These appear in the visited-rooms display call to `RegionMap.Draw(...)` and similar. Make sure the initial env/player construction still uses the string `"Map1"` / `1` directly (or read from `GameState` after `Reset()`):

```csharp
GameState.Reset();
var env    = new Env(GameState.CurrentRoom, GameState.CurrentRegion);
var player = new Hero(env, (2, 10));
```

**Step 3: Replace the local `HandleRoomTransition` call**

Find the call site (inside the game loop):

```csharp
if (env.IsOutsideMap(player.Fallbox()))
    HandleRoomTransition(ref env, ref player, ref camera, ref currentRoom,
                         ref currentRegion, ref currentSong, ref currentMusic,
                         Env.AllTransitions, visitedRooms, roomCache, cacheQueue);
```

Replace with:

```csharp
if (env.IsOutsideMap(player.Fallbox()))
{
    GameState.HandleRoomTransition(ref env, ref player, ref camera);
    currentSong = TryStartMusic(env, ref currentMusic, currentSong);
}
```

**Step 4: Delete the old local function**

Remove the entire `void HandleRoomTransition(...)` local function (from its signature down to its closing `}`). It runs from roughly line 416 to 511.

**Step 5: Build**

```bash
dotnet build 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

**Step 6: Run all tests**

```bash
dotnet test -v normal
```

Expected: all tests pass.

**Step 7: Commit**

```bash
git add worldtree-raylib/src/Program.cs
git commit -m "refactor: replace HandleRoomTransition local function with GameState.HandleRoomTransition"
```
