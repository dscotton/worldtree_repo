// src/Program.cs
using Raylib_cs;
using System.Numerics;
using WorldTree;
using Env = WorldTree.Environment;

// Working directory: dotnet run sets this to the project dir automatically
Raylib.InitWindow(GameConstants.ScreenWidth, GameConstants.ScreenHeight, GameConstants.GameName);
Raylib.InitAudioDevice();
Raylib.SetTargetFPS(60);

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
}

void RunGame()
{
    var allTransitions = MapLoader.LoadTransitions("data/map_transitions.json");

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

    while (!Raylib.WindowShouldClose() && gameState == GameState.Playing)
    {
        if (currentMusic.HasValue) Raylib.UpdateMusicStream(currentMusic.Value);

        // --- Update ---
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
        Raylib.BeginDrawing();
        Raylib.ClearBackground(env.BgColor);

        Raylib.BeginMode2D(camera);
        env.DrawTiles();
        foreach (var i in env.ItemGroup)    i.Draw();
        foreach (var e in env.EnemyGroup)   e.Draw();
        foreach (var b in env.HeroProjectileGroup)  b.Draw();
        foreach (var b in env.EnemyProjectileGroup) b.Draw();
        foreach (var d in env.DyingAnimationGroup)  d.Draw();
        if (!player.IsDead) player.Draw();
        Raylib.EndMode2D();

        statusbar.Draw(); // screen-space HUD

        if (gameState == GameState.GameOver)
        {
            var font = Raylib.LoadFont(Path.Combine(GameConstants.FontDir, GameConstants.Font));
            var text = "Game Over";
            var size = Raylib.MeasureTextEx(font, text, 24, 1);
            Raylib.DrawTextEx(font, text,
                new Vector2(GameConstants.ScreenWidth / 2f - size.X / 2f,
                            GameConstants.ScreenHeight / 2f - size.Y / 2f),
                24, 1, Color.White);
        }

        Raylib.EndDrawing();

        // --- Room transitions ---
        if (env.IsOutsideMap(player.Fallbox()))
            HandleRoomTransition(ref env, ref player, ref camera, ref currentRoom,
                                 ref currentRegion, ref currentSong, ref currentMusic,
                                 allTransitions);
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
    // Manually building this based on environment.py SONGS dict
    var dict = new Dictionary<int, Dictionary<string, string>>();
    
    // Region 1
    dict[1] = new Dictionary<string, string>();
    string[] map1Songs = { "Map1", "Map2", "Map3", "Map4", "Map5", "Map6", "Map7" };
    foreach (var m in map1Songs) dict[1][m] = "june_breeze.ogg";
    
    dict[1]["Map8"] = "photosynthesis.ogg";
    dict[1]["Map9"] = "photosynthesis.ogg";
    dict[1]["Map10"] = "photosynthesis.ogg";
    dict[1]["Map11"] = "foreboding_cave.ogg";
    dict[1]["Map12"] = "foreboding_cave.ogg";
    dict[1]["Map13"] = "foreboding_cave.ogg";
    dict[1]["Map14"] = "foreboding_cave.ogg";
    dict[1]["Map15"] = "nighttime.ogg";
    dict[1]["Map16"] = "nighttime.ogg";
    dict[1]["Map17"] = "nighttime.ogg";
    dict[1]["Map18"] = "nighttime.ogg";

    // Region 2
    dict[2] = new Dictionary<string, string>();
    dict[2]["Map1"] = "ozor.ogg";
    dict[2]["Map2"] = "ozor.ogg";
    dict[2]["Map3"] = "ozor.ogg";
    dict[2]["Map4"] = "ozor.ogg";
    dict[2]["Map5"] = "bongo_wip.ogg"; // Boss room
    
    return dict;
}

Dictionary<int, Dictionary<string, Color>> BuildBgColorsByRoom()
{
    var dict = new Dictionary<int, Dictionary<string, Color>>();
    dict[1] = new Dictionary<string, Color>();
    
    var blue = GameConstants.ColorBlue;
    var black = GameConstants.ColorBlack;

    string[] blueMaps = { "Map1", "Map2", "Map3", "Map4", "Map5", "Map6", "Map7", "Map8", "Map9", "Map10" };
    foreach (var m in blueMaps) dict[1][m] = blue;

    string[] blackMaps = { "Map11", "Map12", "Map13", "Map14", "Map15", "Map16", "Map17", "Map18" };
    foreach (var m in blackMaps) dict[1][m] = black;

    dict[2] = new Dictionary<string, Color>();
    string[] r2Maps = { "Map1", "Map2", "Map3", "Map4", "Map5" };
    foreach (var m in r2Maps) dict[2][m] = black;

    return dict;
}

void HandleRoomTransition(ref Env env, ref Hero player, ref Camera2D camera, 
                          ref string currentRoom, ref int currentRegion, 
                          ref string? currentSong, ref Music? currentMusic,
                          Dictionary<int, Dictionary<string, Dictionary<TransitionDirection, List<TransitionInfo>>>> transitions)
{
    // Determine direction of exit
    TransitionDirection dir;
    if (player.Rect.CenterX() < 0) dir = TransitionDirection.Left;
    else if (player.Rect.CenterX() > env.Width * GameConstants.TileWidth) dir = TransitionDirection.Right;
    else if (player.Rect.CenterY() < 0) dir = TransitionDirection.Up;
    else dir = TransitionDirection.Down;

    if (transitions.TryGetValue(currentRegion, out var regionTrans) &&
        regionTrans.TryGetValue(currentRoom, out var roomTrans) &&
        roomTrans.TryGetValue(dir, out var transList))
    {
        // Find matching transition based on coordinate
        // Horizontal exit -> check Y coordinate (row)
        // Vertical exit -> check X coordinate (col)
        
        int pCol = (int)(player.Rect.CenterX() / GameConstants.TileWidth);
        int pRow = (int)(player.Rect.CenterY() / GameConstants.TileHeight);
        
        // Clamp to map bounds for safety
        pCol = Math.Clamp(pCol, 0, env.Width - 1);
        pRow = Math.Clamp(pRow, 0, env.Height - 1);

        TransitionInfo? match = null;
        foreach (var t in transList)
        {
            if (dir == TransitionDirection.Left || dir == TransitionDirection.Right)
            {
                if (pRow >= t.First && pRow <= t.Last) { match = t; break; }
            }
            else
            {
                if (pCol >= t.First && pCol <= t.Last) { match = t; break; }
            }
        }

        if (match != null)
        {
            currentRegion = match.Region;
            currentRoom = match.Dest;
            
            env = new Env(currentRoom, currentRegion);
            
            // Calculate new position
            int newCol = 0, newRow = 0;
            if (dir == TransitionDirection.Left)
            {
                newCol = env.Width - 2;
                newRow = pRow + match.Offset;
            }
            else if (dir == TransitionDirection.Right)
            {
                newCol = 1;
                newRow = pRow + match.Offset;
            }
            else if (dir == TransitionDirection.Up)
            {
                newCol = pCol + match.Offset;
                newRow = env.Height - 2;
            }
            else if (dir == TransitionDirection.Down)
            {
                newCol = pCol + match.Offset;
                newRow = 1;
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
