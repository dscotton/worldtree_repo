// src/RegionMap.cs
using Raylib_cs;
using System.Numerics;

namespace WorldTree;

/// <summary>
/// Computes and draws the region map shown on the pause screen.
/// Room positions are derived from transition data via BFS, using the
/// offset field to correctly align connected rooms.
/// </summary>
public static class RegionMap
{
    // Precomputed tile-coordinate top-left positions for every room, per region.
    // Populated once by ComputeLayouts() after map data is loaded.
    public static Dictionary<int, Dictionary<string, (int x, int y)>> Layouts = new();

    private const float PanelW   = 720f;
    private const float PanelH   = 480f;
    private const float Padding  = 24f;
    private const float TitleH   = 28f; // height reserved for the "MAP" header

    /// <summary>Call once after Environment.Regions and AllTransitions are populated.</summary>
    public static void ComputeLayouts()
    {
        foreach (var (region, rooms) in Environment.Regions)
        {
            if (!Environment.AllTransitions.TryGetValue(region, out var transitions)) continue;
            if (!rooms.ContainsKey("Map1")) continue;
            Layouts[region] = ComputeLayout(region, "Map1", rooms, transitions);
        }
    }

    private static Dictionary<string, (int x, int y)> ComputeLayout(
        int region,
        string startRoom,
        Dictionary<string, MapInfo> rooms,
        Dictionary<string, Dictionary<TransitionDirection, List<TransitionInfo>>> transitions)
    {
        var layout = new Dictionary<string, (int x, int y)> { [startRoom] = (0, 0) };
        var queue  = new Queue<string>();
        queue.Enqueue(startRoom);

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            var (rx, ry) = layout[room];
            int rw = rooms[room].Width;
            int rh = rooms[room].Height;

            if (!transitions.TryGetValue(room, out var roomTrans)) continue;

            foreach (var (dir, transList) in roomTrans)
            {
                foreach (var t in transList)
                {
                    if (t.Region != region) continue;           // skip cross-region
                    if (layout.ContainsKey(t.Dest))  continue;  // already placed
                    if (!rooms.ContainsKey(t.Dest))  continue;  // unknown room

                    int dw  = rooms[t.Dest].Width;
                    int dh  = rooms[t.Dest].Height;
                    int off = t.Offset;

                    // offset encodes how the exit rows/cols align with entry rows/cols:
                    //   destRow = srcRow + offset  →  destTopY = srcTopY - offset  (for L/R exits)
                    //   destCol = srcCol + offset  →  destLeftX = srcLeftX - offset (for U/D exits)
                    (int dx, int dy) = dir switch
                    {
                        TransitionDirection.Right => (rx + rw, ry - off),
                        TransitionDirection.Left  => (rx - dw, ry - off),
                        TransitionDirection.Down  => (rx - off, ry + rh),
                        TransitionDirection.Up    => (rx - off, ry - dh),
                        _                         => (rx, ry),
                    };

                    layout[t.Dest] = (dx, dy);
                    queue.Enqueue(t.Dest);
                }
            }
        }

        return layout;
    }

    /// <summary>
    /// Draw the map panel. Call this in screen space (outside BeginMode2D).
    /// </summary>
    public static void Draw(int region, HashSet<string> visited, string currentRoom)
    {
        if (!Layouts.TryGetValue(region, out var layout)) return;
        if (!Environment.Regions.TryGetValue(region, out var rooms)) return;

        // Panel position (centered on screen)
        float panelX = (GameConstants.ScreenWidth  - PanelW) / 2f;
        float panelY = (GameConstants.ScreenHeight - PanelH) / 2f;

        Raylib.DrawRectangle((int)panelX, (int)panelY, (int)PanelW, (int)PanelH, Color.Black);
        Raylib.DrawRectangleLines((int)panelX, (int)panelY, (int)PanelW, (int)PanelH, Color.White);

        // "MAP" title centred in the header strip
        var title = "MAP";
        var titleSize = Raylib.MeasureTextEx(GameConstants.GameOverFont, title, 16, 1);
        Raylib.DrawTextEx(GameConstants.GameOverFont, title,
            new Vector2(panelX + (PanelW - titleSize.X) / 2f, panelY + (TitleH - titleSize.Y) / 2f),
            16, 1, Color.White);

        // Bounding box of ALL rooms in the region → consistent scale regardless of visited set
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var (room, (rx, ry)) in layout)
        {
            if (!rooms.TryGetValue(room, out var info)) continue;
            minX = Math.Min(minX, rx);
            minY = Math.Min(minY, ry);
            maxX = Math.Max(maxX, rx + info.Width);
            maxY = Math.Max(maxY, ry + info.Height);
        }

        // Drawing area below the title header
        float drawX = panelX + Padding;
        float drawY = panelY + TitleH + Padding / 2f;
        float drawW = PanelW - Padding * 2f;
        float drawH = PanelH - TitleH - Padding * 1.5f;

        float extentW = maxX - minX;
        float extentH = maxY - minY;
        float scale   = Math.Min(drawW / extentW, drawH / extentH);

        // Centre the scaled map within the drawing area
        float mapW = extentW * scale;
        float mapH = extentH * scale;
        float originX = drawX + (drawW - mapW) / 2f;
        float originY = drawY + (drawH - mapH) / 2f;

        // Draw each visited room
        foreach (var room in visited)
        {
            if (!layout.TryGetValue(room, out var pos)) continue;
            if (!rooms.TryGetValue(room, out var info)) continue;

            float rx = originX + (pos.x - minX) * scale;
            float ry = originY + (pos.y - minY) * scale;
            float rw = MathF.Ceiling(info.Width  * scale);
            float rh = MathF.Ceiling(info.Height * scale);

            // Fill the current room subtly, outline all visited rooms
            if (room == currentRoom)
                Raylib.DrawRectangle((int)rx, (int)ry, (int)rw, (int)rh,
                    new Color(0xFF, 0xFF, 0x00, 0x40)); // dim yellow fill
            Raylib.DrawRectangleLines((int)rx, (int)ry, (int)rw, (int)rh,
                room == currentRoom ? Color.Yellow : Color.White);
        }
    }
}
