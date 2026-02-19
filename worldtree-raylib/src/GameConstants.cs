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
}

// Input action constants
public enum InputAction
{
    Up = 1, Down = 2, Left = 3, Right = 4,
    Jump = 5, Attack = 6, Start = 7, Shoot = 8
}

// Game state — replaces GameOverException / GameWonException
public enum GameState { Playing, GameOver, Won }
