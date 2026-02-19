// src/characters/Powerups.cs (stub)
using Raylib_cs;

namespace WorldTree;

public abstract class Powerup
{
    public Rectangle Rect;
    public bool IsDead { get; protected set; }
    protected Environment Env;
    private readonly bool _oneTime;
    private static Sound? _itemGetSound;
    protected static Sound ItemGetSound =>
        _itemGetSound ??= Raylib.LoadSound("media/music/item_get.ogg");

    protected Powerup(Environment env, (int col, int row) position, bool oneTime = true)
    {
        Env = env;
        _oneTime = oneTime;
        var tileRect = env.RectForTile(position.col, position.row);
        Rect = new Rectangle(tileRect.X, tileRect.Y, 48, 48);
        InitImages();
    }

    protected virtual void InitImages() { }
    public virtual void Update() { }

    public Rectangle Hitbox() =>
        new Rectangle(Rect.X + 3, Rect.Y + 3, Rect.Width - 6, Rect.Height - 6);

    public void PickUp(Hero player)
    {
        Use(player);
        if (_oneTime) IsDead = true;
    }

    protected abstract void Use(Hero player);

    public virtual void Draw() { } // most powerups draw their texture here
}

public static class Powerups
{
    public class HealthBoost : Powerup
    {
        private static Texture2D[]? _imgs;
        private Animation<Texture2D> _anim;
        private static Texture2D[] Imgs => _imgs ??=
            TextureCache.LoadImages("lifeup*.png", scaled: true, colorkey: true);

        public HealthBoost(Environment env, (int, int) pos) : base(env, pos, oneTime: true)
        { _anim = new Animation<Texture2D>(Imgs, frameDelay: 1); }

        protected override void InitImages() { }
        public override void Update() { }
        protected override void Use(Hero p) { p.RaiseMaxHp(5); Raylib.PlaySound(ItemGetSound); }
        public override void Draw() =>
            Raylib.DrawTexture(_anim.NextFrame(), (int)Rect.X, (int)Rect.Y, Color.White);
    }

    public class HealthRestore : Powerup
    {
        private static Texture2D[]? _imgs;
        private Animation<Texture2D> _anim;
        private static Texture2D[] Imgs => _imgs ??=
            TextureCache.LoadImages("healthrestore*.png", scaled: true, colorkey: true);

        public HealthRestore(Environment env, (int, int) pos) : base(env, pos)
        { _anim = new Animation<Texture2D>(Imgs, frameDelay: 1); }

        protected override void InitImages() { }
        public override void Update() { }
        protected override void Use(Hero p) => p.RecoverHealth(3);
        public override void Draw() =>
            Raylib.DrawTexture(_anim.NextFrame(), (int)Rect.X, (int)Rect.Y, Color.White);
    }

    public class DoubleJump : Powerup
    {
        public DoubleJump(Environment env, (int, int) pos) : base(env, pos, oneTime: true) { }
        protected override void Use(Hero p) { p.MaxJumps = 2; Raylib.PlaySound(ItemGetSound); }
    }

    public class MoreSeeds : Powerup
    {
        private static Texture2D[]? _imgs;
        private Animation<Texture2D> _anim;
        private static Texture2D[] Imgs => _imgs ??=
            TextureCache.LoadImages("ammoup*.png", scaled: true, colorkey: true);

        public MoreSeeds(Environment env, (int, int) pos) : base(env, pos, oneTime: true)
        { _anim = new Animation<Texture2D>(Imgs, frameDelay: 1); }

        protected override void InitImages() { }
        public override void Update() { }
        protected override void Use(Hero p)
        {
            p.MaxAmmo += 2;
            p.Ammo = Math.Min(p.MaxAmmo, p.Ammo + 2);
            Raylib.PlaySound(ItemGetSound);
        }
        public override void Draw() =>
            Raylib.DrawTexture(_anim.NextFrame(), (int)Rect.X, (int)Rect.Y, Color.White);
    }

    public class AmmoRestore : Powerup
    {
        private static Texture2D[]? _imgs;
        private Animation<Texture2D> _anim;
        private static Texture2D[] Imgs => _imgs ??=
            TextureCache.LoadImages("seedammo*.png", scaled: true, colorkey: true);

        public AmmoRestore(Environment env, (int, int) pos) : base(env, pos)
        { _anim = new Animation<Texture2D>(Imgs, frameDelay: 1); }

        protected override void InitImages() { }
        public override void Update() { }
        protected override void Use(Hero p) => p.Ammo = Math.Min(p.MaxAmmo, p.Ammo + 2);
        public override void Draw() =>
            Raylib.DrawTexture(_anim.NextFrame(), (int)Rect.X, (int)Rect.Y, Color.White);
    }

    public class Lava : Powerup
    {
        private const int LavaDamage = 2;
        public Lava(Environment env, (int col, int row) pos, int widthInTiles)
            : base(env, pos, oneTime: false)
        {
            Rect = Rect.WithSize(widthInTiles * GameConstants.TileWidth, GameConstants.TileHeight);
        }
        protected override void Use(Hero p) { if (p.Invulnerable == 0) p.TakeHit(LavaDamage); }
    }

    public class Spike : Powerup
    {
        private const int SpikeDamage = 2;
        private const float SpikePushback = 32f;
        public Spike(Environment env, (int col, int row) pos, int widthInTiles)
            : base(env, pos, oneTime: false)
        {
            Rect = Rect.WithSize(widthInTiles * GameConstants.TileWidth, GameConstants.TileHeight);
        }
        protected override void Use(Hero p)
        {
            if (p.Invulnerable == 0) { p.CollisionPushback(new SpikeProxy(Rect)); p.TakeHit(SpikeDamage); }
        }

        // Minimal proxy so CollisionPushback can use Spike's rect
        private class SpikeProxy : Character
        {
            public SpikeProxy(Rectangle r) : base(null!, (0, 0)) { Rect = r; }
            public override float PushBack => SpikePushback;
            protected override void InitImages() { }
            protected override void SetCurrentImage() { }
            protected override (float, float) GetMove() => (0, 0);
        }
    }
}
