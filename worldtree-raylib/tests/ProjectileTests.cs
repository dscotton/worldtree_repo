using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class ProjectileTests
{
    private class TestProjectile : Projectile
    {
        public TestProjectile(Environment env, int damage, float speed, (float x, float y) dir, (float x, float y) pos)
            : base(env, damage, speed, dir, pos) { }

        protected override void InitImages()
        {
            // Set dummy image size
            _currentImage = new Texture2D { Width = 10, Height = 10 };
            Rect = new Rectangle(Rect.X, Rect.Y, 10, 10);
        }
    }

    private class TestCharacter : Character
    {
        public TestCharacter(Environment env, (int c, int r) p) : base(env, p) { }
        protected override void InitImages() {}
        protected override void SetCurrentImage() {}
        protected override (float x, float y) GetMove() => (0,0);
        public override void Die() { Kill(); } // Override to avoid DyingAnimation
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
    public void Update_MovesProjectile()
    {
        var env = CreateTestEnvironment(10, 10);
        // Speed 5, Direction (1, 0) -> Velocity (5, 0)
        var proj = new TestProjectile(env, 1, 5f, (1, 0), (10, 10));
        
        Assert.Equal(10, proj.Rect.X);
        
        proj.Update();
        Assert.Equal(15, proj.Rect.X);
        Assert.Equal(10, proj.Rect.Y);
        Assert.False(proj.IsDead);
    }

    [Fact]
    public void Update_KillsOnWallCollision()
    {
        // Environment with a wall at x=20
        var width = 5;
        var height = 5;
        var grid = new Tile[width][];
        for (int i = 0; i < width; i++) grid[i] = new Tile[height];
        
        // Col 1 (x=48..95) is SolidLeft
        grid[1][0] = new Tile(null, true, false, false, false);
        grid[0][0] = Tile.Empty;
        
        var env = new Environment(width, height, grid);

        // Projectile at x=35, moving right.
        // Wall at x=48.
        // Right edge at 45.
        // Speed 10. Next pos x=45. Right edge 55.
        // 45 < 48 (true), 55 >= 48 (true) -> Collision.
        var proj = new TestProjectile(env, 1, 10f, (1, 0), (35, 10)); // y=10 is row 0
        
        proj.Update();
        Assert.True(proj.IsDead);
    }

    [Fact]
    public void CollideWith_DamagesCharacter()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new TestCharacter(env, (0, 0)); // hp=1
        var proj = new TestProjectile(env, 10, 5f, (1, 0), (0, 0));
        
        Assert.Equal(1, chara.Hp);
        
        proj.CollideWith(chara);
        
        Assert.Equal(-9, chara.Hp);
        Assert.True(chara.IsDead);
    }
}
