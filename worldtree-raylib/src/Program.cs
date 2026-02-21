// src/Program.cs
using Raylib_cs;
using System.Numerics;
using WorldTree;
using Env = WorldTree.Environment;

// Working directory: dotnet run sets this to the project dir automatically
Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.InitWindow(GameConstants.ScreenWidth, GameConstants.ScreenHeight, GameConstants.GameName);
Raylib.InitAudioDevice();
Raylib.SetTargetFPS(60);

// Rooms within the cache retain live entity state on re-entry (enemies stay
// dead, dropped items persist). Rooms that fall out are reloaded fresh so
// enemies respawn. One-time powerups never respawn regardless, because their
// mapcodes are permanently zeroed in Environment.Regions on pickup.
//
// TODO: consider converting to a true LRU cache. Current eviction is FIFO
// (insertion order), so a room visited early may be evicted even if recently
// revisited. An LRU cache would refresh the eviction order on every access.
const int RoomCacheSize = 6;

LoadStaticData();

while (!Raylib.WindowShouldClose())
{
    try { RunGame(); } catch { /* game over — restart */ }
}

Raylib.CloseAudioDevice();
Raylib.CloseWindow();

// ---- local functions ----

void LoadStaticData()
{
    var allMaps1 = MapLoader.LoadRegion("data/map_data.json");
    var allMaps2 = MapLoader.LoadRegion("data/map_data2.json");
    Env.Regions[1] = allMaps1;
    Env.Regions[2] = allMaps2;
    Env.SongsByRoom = BuildSongsByRoom();
    Env.BgColorsByRoom = BuildBgColorsByRoom();
    Env.AllTransitions = MapLoader.LoadTransitions("data/map_transitions.json");
    RegionMap.ComputeLayouts();
    GameConstants.GameOverFont = Raylib.LoadFont(Path.Combine(GameConstants.FontDir, GameConstants.Font));
}

void RunGame()
{
    TitleScreen.ShowTitle();

    string currentRoom = "Map1";
    int currentRegion = 1;
    var env = new Env(currentRoom, currentRegion);
    var player = new Hero(env, (2, 10));
    var statusbar = new Statusbar(player);
    var camera = env.MakeCamera();
    var gameState = GameState.Playing;

    Music? currentMusic = default;
    string? currentSong = null;
    currentSong = TryStartMusic(env, ref currentMusic, null);
    bool debugMode = false;

    var visitedRooms = new Dictionary<int, HashSet<string>>
        { [currentRegion] = new HashSet<string> { currentRoom } };

    var roomCache  = new Dictionary<(int region, string room), Env>();
    var cacheQueue = new Queue<(int region, string room)>();

    while (!Raylib.WindowShouldClose() && gameState is GameState.Playing or GameState.Paused)
    {
        if (currentMusic.HasValue) Raylib.UpdateMusicStream(currentMusic.Value);

        if (Controller.IsActionJustPressed(InputAction.Pause))
            gameState = gameState == GameState.Paused ? GameState.Playing : GameState.Paused;
        if (Controller.IsActionJustPressed(InputAction.Debug))
            debugMode = !debugMode;

        // --- Update (skipped while paused) ---
        if (gameState == GameState.Paused) goto Draw;

        player.HandleInput();

        // Sprite-vs-enemy collision
        foreach (var enemy in env.EnemyGroup.Where(e => !e.IsDead))
        {
            // Use enemy.Hitbox() unless special logic needed
            bool hit = false;
            if (enemy is Enemies.BoomBug bb) hit = Raylib.CheckCollisionRecs(player.Hitbox(), bb.SenseAndReturnHitbox(player));
            else if (enemy is Enemies.Shooter sh) hit = Raylib.CheckCollisionRecs(player.Hitbox(), sh.SenseAndReturnHitbox(player));
            else hit = Raylib.CheckCollisionRecs(player.Hitbox(), enemy.Hitbox());

            if (hit) player.CollideWith(enemy);
        }

        // Item pickup
        foreach (var item in env.ItemGroup.Where(i => !i.IsDead))
            if (Raylib.CheckCollisionRecs(player.Hitbox(), item.Hitbox()))
                item.PickUp(player);

        // Hero projectile vs enemies
        foreach (var bullet in env.HeroProjectileGroup.Where(b => !b.IsDead))
            foreach (var enemy in env.EnemyGroup.Where(e => !e.IsDead))
                if (Raylib.CheckCollisionRecs(bullet.Rect, enemy.Hitbox()))
                { bullet.CollideWith(enemy); bullet.Kill(); }

        // Enemy projectiles vs player
        foreach (var bullet in env.EnemyProjectileGroup.Where(b => !b.IsDead))
            if (Raylib.CheckCollisionRecs(bullet.Rect, player.Hitbox()))
            { bullet.CollideWith(player); bullet.Kill(); }

        player.Update();
        camera = env.Scroll(camera, player.Fallbox());

        foreach (var e in env.EnemyGroup)      e.Update();
        foreach (var i in env.ItemGroup)        i.Update();
        foreach (var b in env.HeroProjectileGroup)  b.Update();
        foreach (var b in env.EnemyProjectileGroup) b.Update();

        foreach (var d in env.DyingAnimationGroup)
        {
            var state = d.Update();
            if (state == GameState.Won) { TitleScreen.ShowCredits(); gameState = GameState.GameOver; }
            else if (state == GameState.GameOver) gameState = GameState.GameOver;
        }

        // Remove dead entities
        env.EnemyGroup.RemoveAll(e => e.IsDead);
        env.ItemGroup.RemoveAll(i => i.IsDead);
        env.HeroProjectileGroup.RemoveAll(b => b.IsDead);
        env.EnemyProjectileGroup.RemoveAll(b => b.IsDead);
        env.DyingAnimationGroup.RemoveAll(d => d.IsDead);

        // --- Draw ---
        Draw:
        Raylib.BeginDrawing();
        Raylib.ClearBackground(env.BgColor);

        Raylib.BeginMode2D(camera);
        env.DrawTiles();
        if (debugMode) env.DrawDebugBounds();
        foreach (var i in env.ItemGroup)    i.Draw();
        foreach (var e in env.EnemyGroup)   e.Draw();
        foreach (var b in env.HeroProjectileGroup)  b.Draw();
        foreach (var b in env.EnemyProjectileGroup) b.Draw();
        foreach (var d in env.DyingAnimationGroup)  d.Draw();
        if (!player.IsDead) player.Draw();
        Raylib.EndMode2D();

        statusbar.Draw(); // screen-space HUD

        if (gameState == GameState.Paused)
            RegionMap.Draw(currentRegion,
                visitedRooms.GetValueOrDefault(currentRegion, new HashSet<string>()),
                currentRoom);

        if (gameState == GameState.GameOver)
        {
            var text = "Game Over";
            var size = Raylib.MeasureTextEx(GameConstants.GameOverFont, text, 24, 1);
            Raylib.DrawTextEx(GameConstants.GameOverFont, text,
                new Vector2(GameConstants.ScreenWidth / 2f - size.X / 2f,
                            GameConstants.ScreenHeight / 2f - size.Y / 2f),
                24, 1, Color.White);
        }

        Raylib.EndDrawing();

        // --- Room transitions ---
        if (env.IsOutsideMap(player.Fallbox()))
            HandleRoomTransition(ref env, ref player, ref camera, ref currentRoom,
                                 ref currentRegion, ref currentSong, ref currentMusic,
                                 Env.AllTransitions, visitedRooms, roomCache, cacheQueue);
    }

    if (currentMusic.HasValue) Raylib.StopMusicStream(currentMusic.Value);
}

string? TryStartMusic(Env env, ref Music? currentMusic, string? currentSong)
{
    // Check if music changed
    string? newSong = null;
    if (Env.SongsByRoom.TryGetValue(env.Region, out var songs) && 
        songs.TryGetValue(env.Name, out var song))
    {
        newSong = song;
    }

    if (newSong != currentSong)
    {
        if (currentMusic.HasValue) Raylib.StopMusicStream(currentMusic.Value);
        if (newSong != null)
        {
            currentMusic = Raylib.LoadMusicStream(Path.Combine(GameConstants.MusicDir, newSong));
            Raylib.PlayMusicStream(currentMusic.Value);
        }
        else
        {
            currentMusic = null;
        }
        return newSong;
    }
    return currentSong;
}

Dictionary<int, Dictionary<string, string>> BuildSongsByRoom()
{
    // Mirrors SONGS / SONGS_BY_ROOM from worldtree/environment.py exactly.
    var dict = new Dictionary<int, Dictionary<string, string>>();

    dict[1] = new Dictionary<string, string>();
    foreach (var room in new[] { "Map1","Map2","Map3","Map4","Map5","Map6","Map8","Map9",
                                  "Map11","Map13","Map16","Map30","Map31","Map32" })
        dict[1][room] = "photosynthesis.ogg";
    foreach (var room in new[] { "Map7","Map10","Map12","Map14","Map15","Map17","Map18",
                                  "Map19","Map20","Map21","Map22","Map23","Map24","Map25",
                                  "Map26","Map27","Map28","Map29" })
        dict[1][room] = "foreboding_cave.ogg";

    dict[2] = new Dictionary<string, string>();
    for (int i = 0; i < 25; i++) dict[2][$"Map{i}"] = "nighttime.ogg";
    for (int i = 25; i < 32; i++) dict[2][$"Map{i}"] = "ozor.ogg";
    dict[2]["Map32"] = "bongo_wip.ogg";

    return dict;
}

Dictionary<int, Dictionary<string, Color>> BuildBgColorsByRoom()
{
    // Mirrors BG_COLORS / BG_COLORS_BY_ROOM from worldtree/environment.py exactly.
    var dict = new Dictionary<int, Dictionary<string, Color>>();

    dict[1] = new Dictionary<string, Color>();
    foreach (var room in new[] { "Map1","Map2","Map3","Map4","Map5",
                                  "Map11","Map16","Map30","Map31","Map32" })
        dict[1][room] = GameConstants.ColorBlue;
    foreach (var room in new[] { "Map6","Map7","Map8","Map9","Map10","Map12","Map13","Map14",
                                  "Map15","Map17","Map18","Map19","Map20","Map21","Map22",
                                  "Map23","Map24","Map25","Map26","Map27","Map28","Map29" })
        dict[1][room] = GameConstants.ColorBlack;

    dict[2] = new Dictionary<string, Color>();
    for (int i = 0; i < 33; i++) dict[2][$"Map{i}"] = GameConstants.ColorBlack;

    return dict;
}

void HandleRoomTransition(ref Env env, ref Hero player, ref Camera2D camera,
                          ref string currentRoom, ref int currentRegion,
                          ref string? currentSong, ref Music? currentMusic,
                          Dictionary<int, Dictionary<string, Dictionary<TransitionDirection, List<TransitionInfo>>>> transitions,
                          Dictionary<int, HashSet<string>> visitedRooms,
                          Dictionary<(int region, string room), Env> roomCache,
                          Queue<(int region, string room)> cacheQueue)
{
    // Determine direction of exit using Fallbox (consistent with IsOutsideMap check)
    TransitionDirection dir;
    Rectangle bounds = player.Fallbox();
    if (bounds.CenterX() < 0) dir = TransitionDirection.Left;
    else if (bounds.CenterX() > env.Width * GameConstants.TileWidth) dir = TransitionDirection.Right;
    else if (bounds.CenterY() < 0) dir = TransitionDirection.Up;
    else dir = TransitionDirection.Down;

    if (transitions.TryGetValue(currentRegion, out var regionTrans) &&
        regionTrans.TryGetValue(currentRoom, out var roomTrans) &&
        roomTrans.TryGetValue(dir, out var transList))
    {
                // Use upper-left of hitbox for transition matching (Fix 3)
                var ulTile = env.TileIndexForPoint(player.Hitbox().Left(), player.Hitbox().Top());
                int ulCol = ulTile.col;
                int ulRow = ulTile.row;

                TransitionInfo? match = null;
                foreach (var t in transList)
                {
                    if (dir == TransitionDirection.Left || dir == TransitionDirection.Right)
                    {
                        if (ulRow >= t.First && ulRow <= t.Last) { match = t; break; }
                    }
                    else
                    {
                        if (ulCol >= t.First && ulCol <= t.Last) { match = t; break; }
                    }
                }

                if (match != null)
                {
                    // Save outgoing room to cache before updating currentRegion/currentRoom.
                    var exitKey = (currentRegion, currentRoom);
                    if (!roomCache.ContainsKey(exitKey))
                    {
                        cacheQueue.Enqueue(exitKey);
                        while (cacheQueue.Count > RoomCacheSize)
                            roomCache.Remove(cacheQueue.Dequeue());
                    }
                    roomCache[exitKey] = env;

                    currentRegion = match.Region;
                    currentRoom = match.Dest;
                    visitedRooms.TryAdd(currentRegion, new HashSet<string>());
                    visitedRooms[currentRegion].Add(currentRoom);

                    // Restore cached state or load fresh.
                    if (!roomCache.TryGetValue((currentRegion, currentRoom), out var cachedEnv))
                        cachedEnv = new Env(currentRoom, currentRegion);
                    env = cachedEnv;
                    // Calculate new position — matches worldtree.py:139-176 exactly
            int newCol = 0, newRow = 0;
            if (dir == TransitionDirection.Left)
            {
                newCol = env.Width - 1;
                newRow = ulRow + match.Offset;
            }
            else if (dir == TransitionDirection.Right)
            {
                newCol = 0;
                newRow = ulRow + match.Offset;
            }
            else if (dir == TransitionDirection.Up)
            {
                newCol = ulCol + match.Offset;
                newRow = env.Height - 1;
            }
            else if (dir == TransitionDirection.Down)
            {
                newCol = ulCol + match.Offset;
                newRow = 0;
            }
            
            // Update player
            player.ChangeRooms(env, (newCol, newRow));
            
            // Update camera
            env.SetScreenOffset(player.Rect.CenterX() - GameConstants.ScreenWidth/2f, 
                                player.Rect.CenterY() - GameConstants.ScreenHeight/2f);
            camera = env.MakeCamera(); // Refresh limits
            camera = env.Scroll(camera, player.Fallbox());

            // Update music
            currentSong = TryStartMusic(env, ref currentMusic, currentSong);
        }
    }
}
