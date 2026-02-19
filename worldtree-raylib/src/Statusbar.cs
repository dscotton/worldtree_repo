using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public class Statusbar
{
    private static readonly Dictionary<string, string> RegionNames = new()
    {
        { "photosynthesis.ogg", "High Branches"    },
        { "foreboding_cave.ogg","Ruins of Asgard"  },
        { "nighttime.ogg",      "Inside the Trunk" },
        { "ozor.ogg",           "Star Caves"       },
        { "bongo_wip.ogg",      "The Baron's Lair" },
    };

    private Font _font;
    private Hero _player;

    public Statusbar(Hero player)
    {
        _player = player;
        _font = Raylib.LoadFont(Path.Combine(GameConstants.FontDir, GameConstants.Font));
    }

    /// <summary>
    /// Draw the HUD. Call this OUTSIDE BeginMode2D so coordinates are screen-relative.
    /// </summary>
    public void Draw()
    {
        // Black background strip
        Raylib.DrawRectangle(0, 0, GameConstants.ScreenWidth, GameConstants.MapY, Color.Black);

        string hpText = $"Health: {_player.Hp}/{_player.MaxHp}";
        Raylib.DrawTextEx(_font, hpText, new Vector2(10, 10), 24, 1, Color.White);

        if (_player.MaxAmmo > 0)
        {
            string ammoText = $"Seeds: {_player.Ammo}/{_player.MaxAmmo}";
            Raylib.DrawTextEx(_font, ammoText, new Vector2(10, 45), 24, 1, Color.White);
        }

        if (Environment.SongsByRoom.TryGetValue(_player.Env.Region, out var songs)
            && songs.TryGetValue(_player.Env.Name, out var song)
            && RegionNames.TryGetValue(song, out var regionName))
        {
            var size = Raylib.MeasureTextEx(_font, regionName, 24, 1);
            Raylib.DrawTextEx(_font, regionName,
                new Vector2(GameConstants.ScreenWidth - 10 - size.X, 10), 24, 1, Color.White);
        }

        string roomText = $"Room {_player.Env.Name[3..]}";
        var roomSize = Raylib.MeasureTextEx(_font, roomText, 24, 1);
        Raylib.DrawTextEx(_font, roomText,
            new Vector2(GameConstants.ScreenWidth - 10 - roomSize.X, 45), 24, 1, Color.White);
    }
}
