# Test Coverage Design

## Goal

Improve test coverage for the WorldTree game, focusing on two areas:
1. A `GameState` static class that extracts session-level state out of `RunGame()`, making `HandleRoomTransition` a testable static method.
2. `SetScreenOffset` edge-case tests that directly cover the narrow/short-room crash class.

## Architecture

### `GameState` static class (`src/GameState.cs`)

Holds all session-level state currently captured as locals in `RunGame()`:

```csharp
public static class GameState
{
    public static string CurrentRoom { get; set; }
    public static int CurrentRegion { get; set; }
    public static Dictionary<int, HashSet<string>> VisitedRooms { get; set; }
    public static Dictionary<(int region, string room), Environment> RoomCache { get; set; }
    public static Queue<(int region, string room)> CacheQueue { get; set; }
    public static Music? CurrentMusic { get; set; }
    public static string? CurrentSong { get; set; }

    public const int RoomCacheSize = 6;

    public static void Reset() { /* re-initialise all fields to defaults */ }
}
```

`Reset()` is called at the start of `RunGame()` and by test setup/teardown, ensuring no state leaks between game sessions or test runs.

### `HandleRoomTransition` as a static method on `GameState`

Promoted from a local function in `RunGame()` to a static method. All session state is accessed directly via `GameState.*` rather than through captured locals.

```csharp
public static void HandleRoomTransition(
    Hero player,
    ref Environment env,
    ref Camera2D camera,
    Settings settings)
```

`Program.cs` calls `GameState.HandleRoomTransition(player, ref env, ref camera, settings)` where the local function call used to be.

### No other structural changes

`Environment`, `Hero`, and the existing test helpers are unchanged. Tests use the existing `internal Environment(int width, int height, Tile[][] grid)` constructor and the `TestHero` subclass pattern already established in the test project.

## New Tests

### `GameStateTests.cs` — `HandleRoomTransition`

Setup: call `GameState.Reset()`, populate `Environment.AllTransitions` and `Environment.Regions` with a minimal two-room map, create source and destination `Environment` instances via the test constructor, place the player at the exit edge.

| Test | Assertion |
|------|-----------|
| Player exits right edge → lands in destination room at correct column (0) and matching row | `env.Name == destRoom && player.Rect` is at expected tile position |
| Player exits right edge → `GameState.CurrentRoom` and `GameState.CurrentRegion` updated | state fields reflect new room |
| Player exits right edge → source room saved to `GameState.RoomCache` | cache contains source room key |
| Player exits right edge → destination room restored from cache when previously visited | env is the cached instance, not a fresh load |

Left/up/down exits follow the same pattern; one representative test each is sufficient since the direction logic is symmetric.

### `EnvironmentTests.cs` — `SetScreenOffset` edge cases

All tests use the `internal` test constructor. `MapWidth = 1280`, `MapHeight = 640`, `TileWidth = TileHeight = 48`.

| Test | Room size | Expected X offset | Expected Y offset |
|------|-----------|-------------------|-------------------|
| Narrow room, normal height | 10×20 tiles (480×960px) | (480−1280)/2 = −400 | 0 (clamped, room taller than screen) |
| Normal width, short room | 30×10 tiles (1440×480px) | 0 (clamped) | (480−640)/2 = −80 |
| Both narrow and short | 10×10 tiles (480×480px) | −400 | −80 |
| Exactly screen-sized | 1280/48≈26.7→27×640/48≈13.3→14 tiles | 0 | 0 |
| Larger than screen | 40×20 tiles (1920×960px), player centred | clamped between 0 and 640 | clamped between 0 and 320 |

Each test also asserts that no exception is thrown (the pre-fix crash path).

## Files Changed

| File | Change |
|------|--------|
| `src/GameState.cs` | **New** — static class with session state and `HandleRoomTransition` |
| `src/Program.cs` | Replace local `HandleRoomTransition` function and its captured locals with `GameState.*` calls |
| `tests/GameStateTests.cs` | **New** — `HandleRoomTransition` tests |
| `tests/EnvironmentTests.cs` | Add `SetScreenOffset` edge-case tests |
