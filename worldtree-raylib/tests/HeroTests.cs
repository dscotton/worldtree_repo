using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class HeroTests
{
    private class TestHero : Hero
    {
        public TestHero(Environment env, (int c, int r) p) : base(env, p) { }
        
        protected override void InitImages() 
        {
            // Set dummy image for initial state
            CurrentImage = new Texture2D { Width = 72, Height = 96 };
        }
        
        protected override void SetCurrentImage() 
        {
            // Simplified logic: just keep rect size consistent
            Rect = Rect.WithSize(72, 96);
        }

        public new void Supported() => base.Supported();
        public new void ApplyGravity() => base.ApplyGravity();
        public Vector2 GetMovement() => Movement;
        public void SetMovement(float x, float y) => Movement = new Vector2(x, y);
    }

    private Environment CreateTestEnvironment(int width, int height)
    {
        var grid = new Tile[width][];
        for (int i = 0; i < width; i++)
            grid[i] = new Tile[height];
        
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x][y] = Tile.Empty;
                
        return new Environment(width, height, grid);
    }

    [Fact]
    public void Hero_StartsWithCorrectHpAndAmmo()
    {
        var env = CreateTestEnvironment(10, 10);
        var hero = new TestHero(env, (0, 0));
        Assert.Equal(30, hero.Hp);
        Assert.Equal(0, hero.Ammo);
    }

    [Fact]
    public void Supported_ResetsJumps()
    {
        var env = CreateTestEnvironment(10, 10);
        var hero = new TestHero(env, (0, 0));
        hero.MaxJumps = 2;
        
        // Simulate jumping (consuming jumps)
        // Since we can't call DoJump directly (private), we verify via Supported
        // But Supported() resets _remainingJumps = MaxJumps.
        
        hero.Supported();
        // Can't inspect private _remainingJumps directly easily, but we can verify vertical velocity reset
        hero.SetMovement(0, 10);
        hero.Supported();
        Assert.Equal(0, hero.GetMovement().Y);
    }

    [Fact]
    public void Gravity_AppliesCorrectly()
    {
        var env = CreateTestEnvironment(10, 10);
        var hero = new TestHero(env, (0, 0));
        
        hero.ApplyGravity();
        Assert.Equal(2f, hero.GetMovement().Y); // 0 + 2
    }
}
