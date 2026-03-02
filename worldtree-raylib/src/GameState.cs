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

    public static Dictionary<int, HashSet<string>>                   VisitedRooms { get; set; } = new();
    public static Dictionary<(int region, string room), Environment> RoomCache    { get; set; } = new();
    public static Queue<(int region, string room)>                   CacheQueue   { get; set; } = new();

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
        if      (bounds.CenterX() < 0)                                   dir = TransitionDirection.Left;
        else if (bounds.CenterX() > env.Width * GameConstants.TileWidth)  dir = TransitionDirection.Right;
        else if (bounds.CenterY() < 0)                                   dir = TransitionDirection.Up;
        else                                                               dir = TransitionDirection.Down;

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
