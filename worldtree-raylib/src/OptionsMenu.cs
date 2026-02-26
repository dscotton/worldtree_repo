using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public static class OptionsMenu
{
    // Panel dimensions match RegionMap exactly so the tab bar lines up.
    private const float PanelW  = 720f;
    private const float PanelH  = 480f;
    private const float TitleH  = 40f;
    private const float Padding = 24f;

    private static readonly string[] RowLabels = ["Resolution", "Fullscreen"];

    // -- Drawing ----------------------------------------------------------

    /// <summary>
    /// Draw the options panel content (settings rows).
    /// Call outside BeginMode2D. Does NOT draw the tab bar header -- that is
    /// drawn by the caller so it is shared between Map and Options tabs.
    /// </summary>
    public static void Draw(Settings settings, int selectedRow)
    {
        float panelX = (GameConstants.ScreenWidth  - PanelW) / 2f;
        float panelY = (GameConstants.ScreenHeight - PanelH) / 2f;

        // Panel background + border (same style as RegionMap)
        Raylib.DrawRectangle((int)panelX, (int)panelY, (int)PanelW, (int)PanelH, Color.Black);
        Raylib.DrawRectangleLines((int)panelX, (int)panelY, (int)PanelW, (int)PanelH, Color.White);

        // Content area starts below the shared tab-bar header.
        float contentX = panelX + Padding;
        float contentY = panelY + TitleH + Padding;

        var font = GameConstants.GameOverFont;

        for (int i = 0; i < RowLabels.Length; i++)
        {
            float rowY = contentY + i * 60f;
            // selectedRow == -1 means cursor is on the tab bar; no row highlighted
            var color = (i == selectedRow) ? Color.Yellow : Color.White;

            // Label on the left
            Raylib.DrawTextEx(font, RowLabels[i],
                new Vector2(contentX, rowY), 16, 1, color);

            // Value with arrows on the right (arrows only shown when row is selected)
            string value = i == 0
                ? (i == selectedRow ? $"\u25C4  {settings.Resolution}  \u25BA" : settings.Resolution)
                : (i == selectedRow ? $"\u25C4  {(settings.Fullscreen ? "On" : "Off")}  \u25BA"
                                    : (settings.Fullscreen ? "On" : "Off"));

            var valueSize = Raylib.MeasureTextEx(font, value, 16, 1);
            Raylib.DrawTextEx(font, value,
                new Vector2(panelX + PanelW - Padding - valueSize.X, rowY),
                16, 1, color);
        }

        // Footer hint varies by context
        var footer = selectedRow >= 0
            ? "\u25C4\u25BA change   [Enter] close"
            : "[Down] select   [Enter] close";
        var footerSize = Raylib.MeasureTextEx(font, footer, 12, 1);
        Raylib.DrawTextEx(font, footer,
            new Vector2(panelX + (PanelW - footerSize.X) / 2f, panelY + PanelH - 28f),
            12, 1, Color.DarkGray);
    }

    // -- Input ------------------------------------------------------------

    /// <summary>
    /// Handle options-menu input. Mutates settings and saves on change.
    /// Returns true when the player pressed Enter (Pause key) to close the menu.
    /// </summary>
    public static bool HandleInput(Settings settings, ref int selectedRow)
    {
        if (Controller.IsActionJustPressed(InputAction.Up))
            selectedRow = (selectedRow - 1 + RowLabels.Length) % RowLabels.Length;
        if (Controller.IsActionJustPressed(InputAction.Down))
            selectedRow = (selectedRow + 1) % RowLabels.Length;

        if (Controller.IsActionJustPressed(InputAction.Left) ||
            Controller.IsActionJustPressed(InputAction.Right))
        {
            bool next = Controller.IsActionJustPressed(InputAction.Right);
            if (selectedRow == 0)
            {
                settings.Resolution = next ? settings.NextResolution() : settings.PrevResolution();
                settings.Save();
                ApplyWindowSize(settings);
            }
            else
            {
                settings.Fullscreen = !settings.Fullscreen;
                settings.Save();
                ApplyFullscreen(settings);
            }
        }

        return Controller.IsActionJustPressed(InputAction.Pause);
    }

    // -- Standalone loop (title screen) -----------------------------------

    /// <summary>
    /// Runs a self-contained options menu loop. Used by the title screen.
    /// Draws a simple "OPTIONS" header since there is no shared tab bar here.
    /// Returns when the player presses the Pause/Enter key.
    /// </summary>
    public static void Show(RenderTexture2D canvas, Action<RenderTexture2D> blitFn,
                            Settings settings)
    {
        int selectedRow = 0;
        while (!Raylib.WindowShouldClose())
        {
            // Draw first so blitFn polls input events before we check them,
            // preventing the Enter press that opened this menu from closing it instantly.
            Raylib.BeginTextureMode(canvas);
            Raylib.ClearBackground(Color.Black);

            Draw(settings, selectedRow);

            // Draw a simple centred "OPTIONS" title in the header area (no tab bar here)
            float panelX = (GameConstants.ScreenWidth  - PanelW) / 2f;
            float panelY = (GameConstants.ScreenHeight - PanelH) / 2f;
            var font = GameConstants.GameOverFont;
            var title = "OPTIONS";
            var titleSize = Raylib.MeasureTextEx(font, title, 16, 1);
            Raylib.DrawTextEx(font, title,
                new Vector2(panelX + (PanelW - titleSize.X) / 2f,
                            panelY + (TitleH - titleSize.Y) / 2f),
                16, 1, Color.Yellow);

            Raylib.EndTextureMode();
            blitFn(canvas);

            if (HandleInput(settings, ref selectedRow)) break;
        }
    }

    // -- Private helpers --------------------------------------------------

    private static void ApplyWindowSize(Settings settings)
    {
        if (Raylib.IsWindowFullscreen()) Raylib.ToggleFullscreen();
        var (w, h) = settings.WindowSize();
        Raylib.SetWindowSize(w, h);
    }

    private static void ApplyFullscreen(Settings settings)
    {
        bool isFs = Raylib.IsWindowFullscreen();
        if (settings.Fullscreen && !isFs) Raylib.ToggleFullscreen();
        else if (!settings.Fullscreen && isFs) Raylib.ToggleFullscreen();
    }
}
