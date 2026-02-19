using Raylib_cs;
using System.Numerics;
using WorldTree;

namespace WorldTree.Tests;

public class PowerupTests
{
    private class TestHero : Hero
    {
        public TestHero(Environment env, (int c, int r) p) : base(env, p) { }
        protected override void InitImages() 
        {
            CurrentImage = new Texture2D { Width = 72, Height = 96 };
        }
        protected override void SetCurrentImage() { }
        
        // Expose Hp setter-like behavior for testing damage
        public void SetHp(int hp) 
        {
            // Hp is protected set. We can damage/heal to adjust it, or rely on logic.
            // But we can verify Hp changes relative to MaxHp.
        }
    }

    private class TestPowerup : Powerup
    {
        public bool Used = false;
        public TestPowerup(Environment env, (int c, int r) p) : base(env, p) { }
        protected override void Use(Hero player) { Used = true; }
        protected override void InitImages() { }
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
    public void PickUp_CallsUseAndKillsIfOneTime()
    {
        var env = CreateTestEnvironment(10, 10);
        var hero = new TestHero(env, (0, 0));
        var powerup = new TestPowerup(env, (0, 0)); // oneTime default true
        
        powerup.PickUp(hero);
        
        Assert.True(powerup.Used);
        Assert.True(powerup.IsDead);
    }

    [Fact]
    public void DoubleJump_IncreasesMaxJumps()
    {
        var env = CreateTestEnvironment(10, 10);
        var hero = new TestHero(env, (0, 0));
        hero.MaxJumps = 1;
        
        var powerup = new Powerups.DoubleJump(env, (0, 0));
        // Bypass InitImages/Raylib loading by not drawing it. Logic only.
        // Wait, Constructor calls InitImages. Powerup base calls InitImages.
        // DoubleJump doesn't override InitImages, so it uses base which is empty. 
        // But HealthBoost uses textures in constructor.
        // DoubleJump is safe.
        
        powerup.PickUp(hero);
        Assert.Equal(2, hero.MaxJumps);
    }

    [Fact]
    public void Lava_DamagesHero()
    {
        var env = CreateTestEnvironment(10, 10);
        var hero = new TestHero(env, (0, 0));
        var startHp = hero.Hp;
        
        var lava = new Powerups.Lava(env, (0, 0), 1); // oneTime false
        
        lava.PickUp(hero);
        
        Assert.True(hero.Hp < startHp);
        Assert.False(lava.IsDead);
    }
}
