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

        // Room is 50×40 tiles = 2400×1920 px.
        // maxX = 2400 - 1280 = 1120, maxY = 1920 - 640 = 1280
        env.SetScreenOffset(2000, 2000);
        Assert.Equal(1120, env.ScreenOffset.X);
        Assert.Equal(1280, env.ScreenOffset.Y);

        env.SetScreenOffset(-100, -100);
        Assert.Equal(0, env.ScreenOffset.X);
        Assert.Equal(0, env.ScreenOffset.Y);
    }

    [Fact]
    public void SetScreenOffset_NarrowRoom_CentersX()
    {
        // 10×20 tiles = 480×960 px. Room narrower than MapWidth (1280).
        // maxX = 480 - 1280 = -800 → center = -400. Y is normal: max = 960-640 = 320.
        SetupMockRegion(10, 20);
        var env = new Environment("TestMap", 1);

        env.SetScreenOffset(0, 0);

        Assert.Equal(-400f, env.ScreenOffset.X);
        Assert.Equal(0f,    env.ScreenOffset.Y);
    }

    [Fact]
    public void SetScreenOffset_ShortRoom_CentersY()
    {
        // 30×10 tiles = 1440×480 px. Room shorter than MapHeight (640).
        // maxY = 480 - 640 = -160 → center = -80. X is normal: max = 1440-1280 = 160.
        SetupMockRegion(30, 10);
        var env = new Environment("TestMap", 1);

        env.SetScreenOffset(0, 0);

        Assert.Equal(0f,   env.ScreenOffset.X);
        Assert.Equal(-80f, env.ScreenOffset.Y);
    }

    [Fact]
    public void SetScreenOffset_NarrowAndShortRoom_CentersBothAxes()
    {
        // 10×10 tiles = 480×480 px. Smaller than screen on both axes.
        SetupMockRegion(10, 10);
        var env = new Environment("TestMap", 1);

        env.SetScreenOffset(0, 0);

        Assert.Equal(-400f, env.ScreenOffset.X);
        Assert.Equal(-80f,  env.ScreenOffset.Y);
    }

    [Fact]
    public void SetScreenOffset_LargeRoom_ClampsNormally()
    {
        // 28×16 tiles = 1344×768 px. Larger than screen on both axes.
        // maxX = 1344-1280 = 64, maxY = 768-640 = 128.
        SetupMockRegion(28, 16);
        var env = new Environment("TestMap", 1);

        env.SetScreenOffset(200, 200);

        Assert.Equal(64f,  env.ScreenOffset.X);
        Assert.Equal(128f, env.ScreenOffset.Y);
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

        // Place rect past the right scroll threshold.
        // MapWidth=1280, ScrollMarginX=480 → threshold = 1280-480 = 800.
        var rect = new Rectangle(875, 100, 50, 50); // CenterX = 900
        camera = env.Scroll(camera, rect);

        // New offset.X = 900 - (1280 - 480) = 100
        Assert.Equal(100, env.ScreenOffset.X);
        Assert.Equal(100, camera.Target.X);
    }
}
