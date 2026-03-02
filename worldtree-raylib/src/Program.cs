// src/Program.cs
using Raylib_cs;
using System.Numerics;
using System.Linq;
using WorldTree;
using Env = WorldTree.Environment;

// Working directory: dotnet run sets this to the project dir automatically
Raylib.SetTraceLogLevel(TraceLogLevel.Warning);

var settings = Settings.Load();
var (initW, initH) = settings.WindowSize();

Raylib.InitWindow(initW, initH, GameConstants.GameName);
Raylib.InitAudioDevice();
Raylib.SetTargetFPS(60);

if (settings.Fullscreen) Raylib.ToggleFullscreen();
var renderCanvas = Raylib.LoadRenderTexture(GameConstants.ScreenWidth, GameConstants.ScreenHeight);

// Rooms within the cache retain live entity state on re-entry (enemies stay
// dead, dropped items persist). Rooms that fall out are reloaded fresh so
// enemies respawn. One-time powerups never respawn regardless, because their
// mapcodes are permanently zeroed in Environment.Regions on pickup.
//
// TODO: consider converting to a true LRU cache. Current eviction is FIFO
// (insertion order), so a room visited early may be evicted even if recently
// revisited. An LRU cache would refresh the eviction order on every access.

void BlitToScreen(RenderTexture2D canvas)
{
    float scaleX = Raylib.GetScreenWidth()  / (float)GameConstants.ScreenWidth;
    float scaleY = Raylib.GetScreenHeight() / (float)GameConstants.ScreenHeight;
    float scale  = Math.Min(scaleX, scaleY);
    float destW  = GameConstants.ScreenWidth  * scale;
    float destH  = GameConstants.ScreenHeight * scale;
    float destX  = (Raylib.GetScreenWidth()  - destW) / 2f;
    float destY  = (Raylib.GetScreenHeight() - destH) / 2f;

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);
    Raylib.DrawTexturePro(
        canvas.Texture,
        new Rectangle(0, 0, GameConstants.ScreenWidth, -GameConstants.ScreenHeight),
        new Rectangle(destX, destY, destW, destH),
        Vector2.Zero, 0f, Color.White);
    Raylib.EndDrawing();
}

LoadStaticData();

while (!Raylib.WindowShouldClose())
{
    try { RunGame(); } catch { /* game over -- restart */ }
}

Raylib.UnloadRenderTexture(renderCanvas);
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
    GameConstants.HitFlashShader = Raylib.LoadShader(null, Path.Combine(GameConstants.ShaderDir, "hitflash.frag"));
}

// tabBarSelected: true when the cursor is on the tab bar (Left/Right will switch tabs).
// When false the cursor is on an option row below; the active tab is still highlighted
// but without the cursor arrow.
void DrawPauseTabBar(PauseTab activeTab, bool tabBarSelected)
{
    // The shared panel is 720x480, centred on 1280x720
    const float PanelW = 720f;
    const float TitleH = 40f;
    float panelX = (GameConstants.ScreenWidth  - PanelW) / 2f;
    float panelY = (GameConstants.ScreenHeight - 480f) / 2f;

    var font = GameConstants.GameOverFont;
    string[] tabs = ["MAP", "OPTIONS"];

    // Build labels first so we can measure the total width for centering.
    var labels = new string[tabs.Length];
    for (int i = 0; i < tabs.Length; i++)
    {
        bool active = (i == 0 && activeTab == PauseTab.Map) ||
                      (i == 1 && activeTab == PauseTab.Options);
        labels[i] = (tabBarSelected && active) ? "\u25BA " + tabs[i] : tabs[i];
    }

    const float TabGap = 32f;
    float totalW = 0f;
    for (int i = 0; i < labels.Length; i++)
    {
        totalW += Raylib.MeasureTextEx(font, labels[i], 16, 1).X;
        if (i < labels.Length - 1) totalW += TabGap;
    }

    float tabX = panelX + (PanelW - totalW) / 2f;
    for (int i = 0; i < labels.Length; i++)
    {
        bool active = (i == 0 && activeTab == PauseTab.Map) ||
                      (i == 1 && activeTab == PauseTab.Options);
        var color = active ? Color.Yellow : Color.White;
        var size = Raylib.MeasureTextEx(font, labels[i], 16, 1);
        Raylib.DrawTextEx(font, labels[i],
            new Vector2(tabX, panelY + (TitleH - size.Y) / 2f),
            16, 1, color);
        tabX += size.X + TabGap;
    }
}

void ApplyPauseResolution()
{
    if (Raylib.IsWindowFullscreen()) Raylib.ToggleFullscreen();
    var (w, h) = settings.WindowSize();
    Raylib.SetWindowSize(w, h);
}

void ApplyPauseFullscreen()
{
    bool isFs = Raylib.IsWindowFullscreen();
    if (settings.Fullscreen && !isFs) Raylib.ToggleFullscreen();
    else if (!settings.Fullscreen && isFs) Raylib.ToggleFullscreen();
}

void RunGame()
{
    if (!TitleScreen.ShowTitle(renderCanvas, BlitToScreen, settings)) return;

    GameState.Reset();
    var env = new Env(GameState.CurrentRoom, GameState.CurrentRegion);
    var player = new Hero(env, (2, 10));
    var statusbar = new Statusbar(player);
    var camera = env.MakeCamera();
    var playState = PlayState.Playing;

    Music? currentMusic = default;
    string? currentSong = null;
    currentSong = TryStartMusic(env, ref currentMusic, null);
    bool debugMode = false;

    var pauseTab = PauseTab.Map;
    int pauseOptionsRow = -1; // -1 = cursor on tab bar; >= 0 = cursor on that option row

    while (!Raylib.WindowShouldClose() && playState is PlayState.Playing or PlayState.Paused or PlayState.GameOver)
    {
        if (currentMusic.HasValue) Raylib.UpdateMusicStream(currentMusic.Value);

        // --- Game over: freeze everything, wait for player to acknowledge ---
        if (playState == PlayState.GameOver)
        {
            if (Controller.IsActionJustPressed(InputAction.Pause))
                break;
            goto Draw;
        }

        if (Controller.IsActionJustPressed(InputAction.Pause))
        {
            if (playState == PlayState.Paused)
            {
                playState = PlayState.Playing;
                pauseTab = PauseTab.Map;
                pauseOptionsRow = -1;
            }
            else
            {
                playState = PlayState.Paused;
            }
        }
        if (Controller.IsActionJustPressed(InputAction.Debug))
            debugMode = !debugMode;

        if (playState == PlayState.Paused)
        {
            if (pauseOptionsRow == -1) // cursor is on the tab bar
            {
                // Left/Right switch tabs
                if (Controller.IsActionJustPressed(InputAction.Left) ||
                    Controller.IsActionJustPressed(InputAction.Right))
                    pauseTab = pauseTab == PauseTab.Map ? PauseTab.Options : PauseTab.Map;

                // Down moves cursor into options (Options tab only;
                // Map tab has no selectable options so Down does nothing there)
                if (Controller.IsActionJustPressed(InputAction.Down) &&
                    pauseTab == PauseTab.Options)
                    pauseOptionsRow = 0;
            }
            else // cursor is on an option row
            {
                if (Controller.IsActionJustPressed(InputAction.Up))
                    pauseOptionsRow = pauseOptionsRow == 0 ? -1 : pauseOptionsRow - 1;
                if (Controller.IsActionJustPressed(InputAction.Down))
                    pauseOptionsRow = Math.Min(pauseOptionsRow + 1, 1); // rows 0 and 1

                // Left/Right change the focused row's value
                if (Controller.IsActionJustPressed(InputAction.Left) ||
                    Controller.IsActionJustPressed(InputAction.Right))
                {
                    bool next = Controller.IsActionJustPressed(InputAction.Right);
                    if (pauseOptionsRow == 0)
                    {
                        settings.Resolution = next ? settings.NextResolution() : settings.PrevResolution();
                        settings.Save();
                        ApplyPauseResolution();
                    }
                    else
                    {
                        settings.Fullscreen = !settings.Fullscreen;
                        settings.Save();
                        ApplyPauseFullscreen();
                    }
                }
            }
            goto Draw;
        }

        // --- Update (skipped while paused) ---
        player.HandleInput();

        // Hitstop: freeze all entity updates for a few frames when a melee hit lands.
        // HandleInput still runs (so _attacking ticks and inputs are buffered),
        // but nothing moves -- creating the classic "weight on impact" feel.
        if (env.HitStop > 0) { env.HitStop--; goto Draw; }

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
            if (state == PlayState.Won) { TitleScreen.ShowCredits(renderCanvas, BlitToScreen); playState = PlayState.GameOver; }
            else if (state == PlayState.GameOver) playState = PlayState.GameOver;
        }

        // Remove dead entities
        env.EnemyGroup.RemoveAll(e => e.IsDead);
        env.ItemGroup.RemoveAll(i => i.IsDead);
        env.HeroProjectileGroup.RemoveAll(b => b.IsDead);
        env.EnemyProjectileGroup.RemoveAll(b => b.IsDead);
        env.DyingAnimationGroup.RemoveAll(d => d.IsDead);

        // --- Draw ---
        Draw:
        Raylib.BeginTextureMode(renderCanvas);
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

        if (playState == PlayState.Paused)
        {
            // Draw panel content first, then tab bar on top so it isn't covered
            if (pauseTab == PauseTab.Map)
                RegionMap.Draw(GameState.CurrentRegion,
                    GameState.VisitedRooms.GetValueOrDefault(GameState.CurrentRegion, new HashSet<string>()),
                    GameState.CurrentRoom,
                    compassActive: player.CompassRegions.Contains(GameState.CurrentRegion));
            else
                OptionsMenu.Draw(settings, pauseOptionsRow);
            DrawPauseTabBar(pauseTab, tabBarSelected: pauseOptionsRow == -1);
        }

        if (playState == PlayState.GameOver)
        {
            var text = "Game Over";
            var size = Raylib.MeasureTextEx(GameConstants.GameOverFont, text, 24, 1);
            Raylib.DrawTextEx(GameConstants.GameOverFont, text,
                new Vector2(GameConstants.ScreenWidth / 2f - size.X / 2f,
                            GameConstants.ScreenHeight / 2f - size.Y / 2f),
                24, 1, Color.White);
        }

        Raylib.EndTextureMode();
        BlitToScreen(renderCanvas);

        // --- Room transitions ---
        if (env.IsOutsideMap(player.Fallbox()))
        {
            GameState.HandleRoomTransition(ref env, ref player, ref camera);
            currentSong = TryStartMusic(env, ref currentMusic, currentSong);
        }
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

