# WorldTree Raylib-cs Port Design

**Date:** 2026-02-18
**Status:** Approved

## Overview

Port the existing Python/PyGame WorldTree platformer to C# with Raylib-cs, targeting .NET 9 on Linux. The goal is a faithful 1:1 structural translation with as little gameplay change as possible, while taking the opportunity to fix two known architectural problems in the original (dual coordinate system, custom rect type). Future passes will add C# idioms (Option B) and potentially an ECS architecture (Option C).

## Project Structure

```
worldtree-raylib/
  WorldTree.csproj
  src/
    Program.cs
    GameConstants.cs
    Controller.cs
    Tile.cs
    Animation.cs
    Environment.cs
    Statusbar.cs
    TitleScreen.cs
    MapTransitions.cs
    characters/
      Character.cs
      Hero.cs
      Enemies.cs
      Powerup.cs
      Projectile.cs
  data/
    map_data.json          # Region 1 maps
    map_data2.json         # Region 2 maps
    map_transitions.json   # Room transition table
  convert_maps.py          # Python script to regenerate JSON from Python source
  media/                   # Symlink or copy of worldtree/media/
```

The Python source files remain untouched. `convert_maps.py` imports them and writes JSON, so the Python files remain the authoritative map source.

## PyGame → Raylib-cs Mapping

| PyGame concept | Raylib-cs equivalent |
|---|---|
| `pygame.Surface` (image) | `Texture2D`, loaded once into a `Dictionary<string, Texture2D>` cache |
| `pygame.Rect` | Raylib `Rectangle` (float x, y, width, height) |
| `pygame.sprite.RenderUpdates` group | `List<T>` per group |
| `surface.blit()` | `Raylib.DrawTexture()` / `DrawTexturePro()` |
| `pygame.transform.flip()` | `DrawTexturePro()` with negative source width |
| `pygame.transform.scale()` | `Raylib.ImageResize()` before uploading to GPU |
| Colorkey transparency (`#FF00FF`) | `Raylib.ImageColorReplace()` at load time |
| `pygame.mixer.Sound` | `Raylib.LoadSound()` / `PlaySound()` |
| `pygame.mixer.music` | `Raylib.LoadMusicStream()` + `UpdateMusicStream()` per frame |
| `pygame.font.Font.render()` | `Raylib.DrawTextEx()` with loaded `Font` |
| `clock.tick(60)` | `Raylib.SetTargetFPS(60)` |

## Coordinate System & Camera

**Problem with original:** The Python code splits between screen coordinates and map coordinates, requiring constant conversions (`ScreenCoordinateForMapPoint`, `MapCoordinateForScreenPoint`). Scrolling works by applying a scroll vector to every sprite's rect every frame, which is fragile.

**Solution:** All entities store world (map) coordinates exclusively. Raylib's `Camera2D` handles the viewport transform:

```csharp
Raylib.BeginMode2D(camera);
// Draw tiles, enemies, player, projectiles — all in world space
Raylib.EndMode2D();
// Draw HUD — always in screen space, no offsets needed
```

Scrolling = updating `camera.Target` to follow the player. The entire family of coordinate conversion methods (`ScreenRectForMapRect`, `MapRectForScreenRect`, etc.) and the per-frame scroll vector application are eliminated.

## Collision Detection

Tile-based directional AABB collision is retained (appropriate for this game style). Per-direction solidity (4-bit bound byte) is preserved exactly.

Sprite-vs-sprite collision uses `Raylib.CheckCollisionRecs()`.

Physics uses `float` coordinates (via Raylib `Rectangle`) for smoother movement rather than integer pixel steps.

## Rendering

- Tiles drawn each frame for the visible tile range — no cached map surface needed (`dirty` flag removed). Camera2D makes this fast enough.
- Sprites drawn with `DrawTexturePro()` — horizontal flip via negative source width, no pre-flipped texture copies.
- HUD drawn after `EndMode2D()` in screen space using `DrawTextEx()` with PressStart2P font.

## Audio

- WAV sound effects: `LoadSound()` / `PlaySound()` / `PlaySoundMulti()` where needed
- OGG music: `LoadMusicStream()` with `UpdateMusicStream()` called each frame in the game loop
- Music fade on room transition: manual `SetMusicVolume()` ramp over ~250ms (4 frames at 60fps ≈ 16ms per step)

## Game State

The Python code uses exceptions (`GameOverException`, `GameWonException`) to signal end states. Replaced with:

```csharp
enum GameState { Playing, GameOver, Won }
```

The `Dying` animation sets state when it completes. The main loop reads state and transitions accordingly. On `GameOver`, JSON map data reloads and a new game begins.

## Map Data Format

Python dicts converted to JSON by `convert_maps.py`:

```json
{
  "Map1": {
    "width": 50,
    "height": 40,
    "tileset": "Tiles2",
    "layout": [[1,1,2,...], ...],
    "bounds": [[15,0,0,...], ...],
    "mapcodes": [[0,0,1,...], ...]
  }
}
```

`map_transitions.json` mirrors the Python structure: `{ "1": { "Map1": { "LEFT": [...], ... } } }`.

Each transition entry: `{ "first": 0, "last": 19, "region": 1, "dest": "Map4", "offset": 0 }`.

## Key Decisions Summary

| Decision | Choice |
|---|---|
| Runtime | .NET 9, Raylib-cs NuGet |
| Architecture | 1:1 Python → C# file mapping (Option A) |
| Coordinate system | World coords + `Camera2D` |
| Rect/collision | Raylib `Rectangle` + float coords |
| Textures | `Texture2D` cache, colorkey at load time |
| Sprite flipping | `DrawTexturePro()` negative width |
| Audio | `LoadSound` (WAV), `LoadMusicStream` (OGG) |
| Map data | JSON, generated by `convert_maps.py` |
| Game state | `GameState` enum, no exceptions |

## Future Work (Out of Scope for This Port)

- **Option B:** Add interfaces (`ICharacter`, `IItem`), generics, `record` types for immutable data
- **Option C:** ECS architecture (entities as IDs, components as structs, systems for logic)
- Proper content pipeline / asset compression
- Gamepad support (the Python controller abstraction already supports this conceptually)
