using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public class TitleScreen
{
    private static readonly string IntroText = """

        The World Tree, source of all
        life...
        
        
        Legend tells of a catastrophic
        battle in which the evil
        Beaver Baron and his legion of
        minions nearly succeeded in
        gnawing down the World Tree
        to make a dam.
        
        
        Over 1000 years have passed
        since the evil Beaver Baron
        was defeated by the protectors
        of the World Tree.  Since that
        time, the World Tree has
        prospered under the care of
        its guardians.
        
        
        But time flows like a river...
        And history repeats...
        
        
        You are Seamus, one of the
        World Tree's guardian spirits.
        Are you a bad enough dude
        to save the World Tree?
        
        """;

    private static readonly string ControlsText = """

        CONTROLS


        Arrow keys or WASD - move

        Space bar - jump

        M - attack

        N - shoot (requires ammo)

        Return - start
        """;

    private static readonly string CreditsText = """

        You have defeated the Beaver
        Baron and brought peace to
        the World Tree.
        Congratulations!  Your name
        echoes through the canopy
        as all the creatures of the
        forest sing your praise.
        
        Fight, Seamus!  For
        everlasting peace!
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        WORLDTREE TEAM
        
        David Scotton
        Neal "Justin" Bailey
        Nicholas Collins
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        See you next mission!
        
        """;

    private Font _font;
    private Texture2D? _background;
    private string _text;
    private int _textSpeed;
    private int _fadeRate;
    private int _frameDelay;
    private Music? _music;

    public TitleScreen(string text, int textSpeed = 4, int fadeRate = 4,
                       int frameDelay = 0, Texture2D? background = null, string? musicFile = null)
    {
        _font = Raylib.LoadFont(Path.Combine(GameConstants.FontDir, GameConstants.Font));
        _text = text;
        _textSpeed = textSpeed;
        _fadeRate = fadeRate;
        _frameDelay = frameDelay;
        _background = background;
        if (musicFile != null)
        {
            _music = Raylib.LoadMusicStream(Path.Combine(GameConstants.MusicDir, musicFile));
            Raylib.PlayMusicStream(_music.Value);
        }
    }

    /// <summary>Show this screen until the player presses Enter. Returns when done.</summary>
    public void Show()
    {
        int frame = -_frameDelay;
        var lines = _text.Split('\n');
        var textArray = new List<string>();
        const int fontHeight = 16;
        const int lineHeight = 20;
        const int textTop = 144;
        const int textBottom = 576;

        while (!Raylib.WindowShouldClose())
        {
            if (_music.HasValue) Raylib.UpdateMusicStream(_music.Value);
            bool startPressed = Controller.GetInput().Contains(InputAction.Pause);
            bool canExit = startPressed && (frame + _frameDelay) >= 30;

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            if (_background.HasValue)
                Raylib.DrawTexture(_background.Value, 0, 0, Color.White);

            if (frame >= 0)
            {
                float textPos = textBottom - frame / (float)_textSpeed;
                int lineIdx = frame / (_textSpeed * lineHeight);
                if (frame % (_textSpeed * lineHeight) == 0 && lineIdx < lines.Length)
                    textArray.Add(lines[lineIdx]);

                // Wrap text drawing in Scissor Mode for pixel-perfect clipping (smooth scrolling)
                Raylib.BeginScissorMode(0, textTop, GameConstants.ScreenWidth, textBottom - textTop);
                
                for (int i = 0; i < textArray.Count; i++)
                {
                    float y = textPos + i * lineHeight;
                    var size = Raylib.MeasureTextEx(_font, textArray[i], fontHeight, 1);
                    Raylib.DrawTextEx(_font, textArray[i],
                        new Vector2(480 - size.X / 2f, y), fontHeight, 1, Color.White);
                }

                Raylib.EndScissorMode();
            }

            Raylib.EndDrawing();

            if (canExit) break;
            frame++;
        }

        if (_music.HasValue) Raylib.StopMusicStream(_music.Value);

        // Fade out
        if (_background.HasValue)
        {
            int fadeFrame = 0;
            while (!Raylib.WindowShouldClose())
            {
                int alpha = 255 - fadeFrame * _fadeRate;
                if (alpha <= 4) break;
                fadeFrame++;
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                Raylib.DrawTexture(_background.Value, 0, 0, new Color((byte)255, (byte)255, (byte)255, (byte)alpha));
                Raylib.EndDrawing();
            }
        }

        if (_music.HasValue) Raylib.UnloadMusicStream(_music.Value);
    }

    public static void ShowTitle()
    {
        var bg = Raylib.LoadTexture(Path.Combine("media", "titlescreen.png"));
        new TitleScreen(IntroText, background: bg, musicFile: "june_breeze.ogg",
                        textSpeed: 4, frameDelay: 120).Show();
        Raylib.UnloadTexture(bg);
        new TitleScreen(ControlsText, textSpeed: 1, fadeRate: 12).Show();
    }

    public static void ShowCredits()
    {
        new TitleScreen(CreditsText, musicFile: "june_breeze_2.ogg", textSpeed: 2).Show();
    }
}
