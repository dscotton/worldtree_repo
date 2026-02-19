// tests/RectangleExtensionsTests.cs
using Raylib_cs;
using WorldTree;

namespace WorldTree.Tests;

public class RectangleExtensionsTests
{
    [Fact]
    public void LeftRightTopBottom()
    {
        var r = new Rectangle(10, 20, 30, 40);
        Assert.Equal(10f, r.Left());
        Assert.Equal(40f, r.Right());
        Assert.Equal(20f, r.Top());
        Assert.Equal(60f, r.Bottom());
    }

    [Fact]
    public void Center()
    {
        var r = new Rectangle(10, 20, 30, 40);
        Assert.Equal(25f, r.CenterX());
        Assert.Equal(40f, r.CenterY());
    }

    [Fact]
    public void Move()
    {
        var r = new Rectangle(10, 20, 30, 40);
        var moved = r.Move(5, -3);
        Assert.Equal(15f, moved.X);
        Assert.Equal(17f, moved.Y);
        Assert.Equal(30f, moved.Width);
        Assert.Equal(40f, moved.Height);
    }

    [Fact]
    public void WithSize()
    {
        var r = new Rectangle(10, 20, 30, 40);
        var resized = r.WithSize(60, 80);
        Assert.Equal(10f, resized.X);
        Assert.Equal(20f, resized.Y);
        Assert.Equal(60f, resized.Width);
        Assert.Equal(80f, resized.Height);
    }
}
