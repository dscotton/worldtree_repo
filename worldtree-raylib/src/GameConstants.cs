// src/GameConstants.cs
using Raylib_cs;

namespace WorldTree;

public static class GameConstants
{
    public const string GameName = "World Tree";
    public const string Font = "PressStart2P.ttf";
    public const int ScreenWidth = 960;
    public const int ScreenHeight = 720;
    public const int MapWidth = 960;
    public const int MapHeight = 640;
    public const int MapX = 0;
    public const int MapY = ScreenHeight - MapHeight;   // 80
    public const int ScrollMarginX = 360;
    public const int ScrollMarginY = 240;
    public const int TileWidth = 48;
    public const int TileHeight = 48;

    public static readonly Color ColorBlack = Color.Black;
    public static readonly Color ColorBlue = new Color(0x10, 0x00, 0x66, 0xFF);
    public static readonly Color ColorWhite = Color.White;
    // Colorkey used in sprites — replaced with transparency at load time
    public static readonly Color SpriteColorkey = new Color(0xFF, 0x00, 0xFF, 0xFF);

    public const string TileDir = "media/tiles";
    public const string MusicDir = "media/music";
    public const string SpritesDir = "media/sprites";
    public const string FontDir = "media/font";

    public static Font GameOverFont;

    // Fragment shader for the hit flash effect.
    // Replaces every non-transparent pixel with the draw tint (Color.White),
    // producing a proper sprite-shaped silhouette rather than a tile-sized rectangle.
    public const string HitFlashFrag = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
out vec4 finalColor;
void main() {
    vec4 texColor = texture(texture0, fragTexCoord);
    finalColor = vec4(fragColor.rgb, texColor.a);
}";

    public static Shader HitFlashShader;
}

// Input action constants
public enum InputAction
{
    Up = 1, Down = 2, Left = 3, Right = 4,
    Jump = 5, Attack = 6, Start = 7, Shoot = 8, Pause = 9, Debug = 10
}

// Game state — replaces GameOverException / GameWonException
public enum GameState { Playing, Paused, GameOver, Won }
