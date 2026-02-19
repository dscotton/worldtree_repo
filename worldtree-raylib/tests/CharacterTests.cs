using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class CharacterTests
{
    // Concrete subclass for testing
    private class TestCharacter : Character
    {
        public TestCharacter(Environment env, (int col, int row) pos) : base(env, pos) { }
        
        protected override void InitImages() 
        {
            // No-op for testing (avoid loading textures)
        }
        
        protected override void SetCurrentImage() { }
        
        // Expose protected methods for testing
        public new void Walk(Direction dir) => base.Walk(dir);
        public new void ApplyGravity() => base.ApplyGravity();
        public new void Supported() => base.Supported();
        
        // Simple move implementation
        protected override (float x, float y) GetMove() => (Movement.X, Movement.Y);
        
        public Vector2 GetMovement() => Movement;
        public void SetMovement(float x, float y) => Movement = new Vector2(x, y);
    }

    private Environment CreateTestEnvironment(int width, int height)
    {
        var grid = new Tile[width][];
        for (int i = 0; i < width; i++)
            grid[i] = new Tile[height];
            
        // Fill with empty tiles
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x][y] = Tile.Empty;
                
        return new Environment(width, height, grid);
    }

    [Fact]
    public void Walk_AcceleratesCorrectly()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new TestCharacter(env, (5, 5));
        // Override default values if needed, but defaults are:
        // Speed=0. Wait, base Character Speed is 0?
        // Let's check Character.cs. Yes, "public virtual float Speed => 0f;"
        // We need to override Speed in TestCharacter to test movement.
    }
    
    // Better TestCharacter with movement stats
    private class MobileCharacter : TestCharacter
    {
        public MobileCharacter(Environment env, (int c, int r) p) : base(env, p) { }
        public override float Speed => 5f;
        public override float Accel => 1f;
        public override float Gravity => 1f;
        public override float TerminalVelocity => 10f;
    }

    [Fact]
    public void Walk_AcceleratesToSpeed()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new MobileCharacter(env, (5, 5));
        
        // Initial movement 0
        Assert.Equal(0, chara.GetMovement().X);
        
        // Walk Right
        chara.Walk(Direction.Right);
        // Accel is 1. Min(0+1, 5) = 1.
        Assert.Equal(1f, chara.GetMovement().X);
        
        // Walk Right again
        chara.Walk(Direction.Right);
        Assert.Equal(2f, chara.GetMovement().X);
    }

    [Fact]
    public void Walk_DeceleratesOrTurns()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new MobileCharacter(env, (5, 5));
        chara.SetMovement(2f, 0f); // Moving right at 2
        
        // Walk Left
        chara.Walk(Direction.Left);
        // Movement > -Speed? Yes.
        // Formula: Max(2 - 1, -5) = 1.
        // It decelerates rightward movement first.
        Assert.Equal(1f, chara.GetMovement().X);
        
        chara.Walk(Direction.Left);
        Assert.Equal(0f, chara.GetMovement().X);
        
        chara.Walk(Direction.Left);
        Assert.Equal(-1f, chara.GetMovement().X);
    }

    [Fact]
    public void ApplyGravity_IncreasesYVelocity()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new MobileCharacter(env, (5, 5));
        
        chara.ApplyGravity();
        Assert.Equal(1f, chara.GetMovement().Y); // 0 + 1
        
        chara.ApplyGravity();
        Assert.Equal(2f, chara.GetMovement().Y);
    }

    [Fact]
    public void ApplyGravity_CapsAtTerminalVelocity()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new MobileCharacter(env, (5, 5));
        chara.SetMovement(0, 9.5f);
        
        chara.ApplyGravity();
        // 9.5 + 1 = 10.5, capped at 10
        Assert.Equal(10f, chara.GetMovement().Y);
    }
    
    [Fact]
    public void Supported_ResetsVerticalVelocity()
    {
        var env = CreateTestEnvironment(10, 10);
        var chara = new MobileCharacter(env, (5, 5));
        chara.SetMovement(5, 10);
        
        chara.Supported();
        Assert.Equal(CharacterAction.Grounded, chara.Vertical);
        Assert.Equal(0f, chara.GetMovement().Y);
    }
}
