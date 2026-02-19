using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class EnvironmentTests
{
    private void SetupMockRegion(int width, int height)
    {
        var mapInfo = new MapInfo
        {
            Width = width,
            Height = height,
            Tileset = "Test",
            Layout = Enumerable.Range(0, height).Select(_ => Enumerable.Repeat(0, width).ToList()).ToList(),
            Bounds = Enumerable.Range(0, height).Select(_ => Enumerable.Repeat(0, width).ToList()).ToList(),
            Mapcodes = Enumerable.Range(0, height).Select(_ => Enumerable.Repeat(0, width).ToList()).ToList()
        };

        if (!Environment.Regions.ContainsKey(1))
            Environment.Regions[1] = new Dictionary<string, MapInfo>();
        
        Environment.Regions[1]["TestMap"] = mapInfo;
    }

    [Fact]
    public void SetScreenOffset_ClampsToBounds()
    {
        SetupMockRegion(50, 40); // 2400x1920 pixels
        var env = new Environment("TestMap", 1);

        // Max offset = (2400 - 960, 1920 - 720) = (1440, 1200)
        // Note: GameConstants.MapHeight is 640, MapWidth is 960. 
        // ScreenHeight is 720, but MapY is 80. Viewport is 960x640.
        // Max offset = (2400 - 960, 1920 - 640) = (1440, 1280)
        
        env.SetScreenOffset(2000, 2000);
        Assert.Equal(1440, env.ScreenOffset.X);
        Assert.Equal(1280, env.ScreenOffset.Y);

        env.SetScreenOffset(-100, -100);
        Assert.Equal(0, env.ScreenOffset.X);
        Assert.Equal(0, env.ScreenOffset.Y);
    }

    [Fact]
    public void IsWorldPointVisible_CorrectChecks()
    {
        SetupMockRegion(50, 40);
        var env = new Environment("TestMap", 1);
        env.SetScreenOffset(100, 100);

        // Viewport is (100, 100) to (1060, 740)
        Assert.True(env.IsWorldPointVisible(150, 150));
        Assert.True(env.IsWorldPointVisible(100, 100));
        Assert.True(env.IsWorldPointVisible(1060, 740));
        
        Assert.False(env.IsWorldPointVisible(50, 50));
        Assert.False(env.IsWorldPointVisible(1100, 800));
    }

    [Fact]
    public void Scroll_FollowsRect()
    {
        SetupMockRegion(100, 100); // Very large map
        var env = new Environment("TestMap", 1);
        var camera = env.MakeCamera();

        // Place rect near right edge of current viewport (960x640)
        // Margin is 360. Right threshold = 0 + 960 - 360 = 600.
        var rect = new Rectangle(700, 100, 50, 50); // CenterX = 725
        
        camera = env.Scroll(camera, rect);
        
        // New offset.X should be 725 - (960 - 360) = 725 - 600 = 125
        Assert.Equal(125, env.ScreenOffset.X);
        Assert.Equal(125, camera.Target.X);
    }
}
