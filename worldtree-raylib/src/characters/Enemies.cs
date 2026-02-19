using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public static class Enemies
{
    // --- 1. Beaver ---
    public class Beaver : Character
    {
        public override int StartingHp => 10;
        public override int Damage => 5;
        public override float Speed => 1f;
        public override float Gravity => 2f;
        public override float TerminalVelocity => 10f;
        public override int Width => 96;
        public override int Height => 60;

        private Animation<Texture2D>? _walkRight, _walkLeft;
        private Texture2D _standRight, _standLeft;
        private int _waiting;

        public Beaver(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var walk = TextureCache.LoadImages("beaver1*.png", scaled: true, colorkey: true);
            _walkRight = new Animation<Texture2D>(walk);
            _walkLeft = new Animation<Texture2D>(walk.Select(TextureCache.FlipHorizontal).ToArray());
            
            _standRight = TextureCache.LoadImage("beaver10000.png", scaled: true, colorkey: true);
            _standLeft = TextureCache.FlipHorizontal(_standRight);

            CurrentImage = _standLeft;
        }

        protected override (float x, float y) GetMove()
        {
            if (_waiting > 0)
            {
                _waiting--;
                Movement.X = 0;
            }
            else
            {
                var move = WalkBackAndForth();
                if (Movement.X == 0) _waiting = 60;
            }
            return (Movement.X, Movement.Y);
        }

        protected override void SetCurrentImage()
        {
            if (Movement.X > 0) CurrentImage = _walkRight!.NextFrame();
            else if (Movement.X < 0) CurrentImage = _walkLeft!.NextFrame();
            else CurrentImage = Facing == Direction.Right ? _standRight : _standLeft;
        }
    }

    // --- 2. Dragonfly ---
    public class Dragonfly : Character
    {
        public override int StartingHp => 2;
        public override int Damage => 3;
        public override float Speed => 16f;
        public override int Width => 48;
        public override int Height => 48;

        private Animation<Texture2D>? _fly;
        private Vector2 _vector = new Vector2(-1, 0);
        private int _moveFrames = 15;
        private int _restFrames = 0;

        public Dragonfly(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("dragonfly*.png", scaled: true, colorkey: true);
            _fly = new Animation<Texture2D>(imgs);
            CurrentImage = _fly.NextFrame();
        }

        protected override (float x, float y) GetMove()
        {
            if (_moveFrames > 0)
            {
                Movement.X = _vector.X * Speed;
                Movement.Y = _vector.Y * Speed;
                _moveFrames--;
                if (_moveFrames == 0)
                    _restFrames = 40 + Raylib.GetRandomValue(0, 20);
            }
            else
            {
                Movement = Vector2.Zero;
                _restFrames--;
                if (_restFrames <= 0)
                {
                    // Rotate 90 degrees CCW: (x, y) -> (-y, x)
                    _vector = new Vector2(-_vector.Y, _vector.X);
                    _moveFrames = (_vector.X != 0) ? 15 : 9;
                }
            }
            return (Movement.X, Movement.Y);
        }

        protected override void SetCurrentImage() => CurrentImage = _fly!.NextFrame();
        
        public override void Update()
        {
            var move = GetMove();
            if (!Env.IsMoveLegal(Hitbox(), (move.x, move.y))) Kill();
            else Rect = Rect.Move(move.x, move.y);
            
            SetCurrentImage();
            if (Env.IsOutsideMap(Hitbox())) Kill();
        }
    }

    // --- 3. BoomBug ---
    public class BoomBug : Character
    {
        public override int StartingHp => 4;
        public override int Damage => _exploding > 0 ? 10 : 1;
        public override float PushBack => _exploding > 0 ? 48f : 16f;
        public override float Speed => 1f;
        public override float Gravity => 2f;
        public override int Width => 48;
        public override int Height => 48;

        private Animation<Texture2D>? _walkRight, _walkLeft, _triggeredRight, _triggeredLeft, _explode;
        private int _triggered;
        private int _exploding;
        private static Sound? _explodeSound;
        private static Sound ExplodeSound => _explodeSound ??= Raylib.LoadSound("media/sfx/explode.wav");

        public BoomBug(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var walk = TextureCache.LoadImages("bombug*.png", scaled: true, colorkey: true);
            _walkRight = new Animation<Texture2D>(walk);
            _walkLeft = new Animation<Texture2D>(walk.Select(TextureCache.FlipHorizontal).ToArray());
            
            var trig = TextureCache.LoadImages("bombexplosionleadup*.png", scaled: true, colorkey: true);
            _triggeredRight = new Animation<Texture2D>(trig);
            _triggeredLeft = new Animation<Texture2D>(trig.Select(TextureCache.FlipHorizontal).ToArray());

            var exp = TextureCache.LoadImages("bombexplode*.png", scaled: true, colorkey: true);
            _explode = new Animation<Texture2D>(exp, frameDelay: 3, looping: false);
            
            CurrentImage = _walkLeft!.NextFrame();
        }

        public Rectangle SenseAndReturnHitbox(Hero hero)
        {
            if (_triggered == 0 && _exploding == 0 && GetDistance(hero) < 160)
            {
                _triggered = 90;
                Movement.X = 0;
            }
            return Hitbox();
        }

        protected override (float x, float y) GetMove()
        {
            if (_triggered > 0)
            {
                Movement.X = 0;
                _triggered--;
                if (_triggered == 0)
                {
                    _exploding = 40;
                    Raylib.PlaySound(ExplodeSound);
                }
            }
            else if (_exploding > 0)
            {
                Movement.X = 0;
                _exploding--;
                if (_exploding == 0) Die();
            }
            else
            {
                WalkBackAndForth();
            }
            return (Movement.X, Movement.Y);
        }

        protected override void SetCurrentImage()
        {
            if (_exploding > 0) CurrentImage = _explode!.NextFrame();
            else if (_triggered > 0) CurrentImage = (Facing == Direction.Right ? _triggeredRight : _triggeredLeft)!.NextFrame();
            else if (Movement.X > 0) CurrentImage = _walkRight!.NextFrame();
            else CurrentImage = _walkLeft!.NextFrame();
        }
        
        public override void Die()
        {
             // BoomBug logic handles death via explosion, but if killed by player before exploding:
             base.Die();
        }
    }

    // --- 4. Shooter ---
    public class Shooter : Character
    {
        public override int StartingHp => 4;
        public override int Damage => 2;
        public override int Width => 48;
        public override int Height => 96;
        
        private Animation<Texture2D>? _idle;
        private int _cooldown;
        private Vector2 _aim = new Vector2(0, -1);

        public Shooter(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("mush*.png", scaled: true, colorkey: true);
            _idle = new Animation<Texture2D>(imgs, frameDelay: 5);
            CurrentImage = _idle.NextFrame();
        }

        public Rectangle SenseAndReturnHitbox(Hero hero)
        {
            if (GetDistance(hero) < 480)
                _aim = new Vector2(hero.Rect.CenterX() - Rect.CenterX(), hero.Rect.CenterY() - Rect.CenterY());
            else
                _aim = new Vector2(0, -1);
            return Hitbox();
        }

        protected override (float x, float y) GetMove() => (0, Movement.Y);

        public override void Update()
        {
            base.Update();
            if (_cooldown > 0) _cooldown--;
            else
            {
                Env.EnemyProjectileGroup.Add(new SporeCloud(Env, (_aim.X, _aim.Y), (Rect.X, Rect.CenterY())));
                _cooldown = 90;
            }
        }

        protected override void SetCurrentImage() => CurrentImage = _idle!.NextFrame();
    }

    // --- 5. PipeBug ---
    public class PipeBug : Character
    {
        public override int StartingHp => 2;
        public override int Damage => 1;
        public override float Speed => 8f;
        public override int Width => 48;
        public override int Height => 48;

        protected Animation<Texture2D>? _anim;
        private bool _turned;

        public PipeBug(Environment env, (int c, int r) pos) : base(env, pos)
        {
            Movement.Y = -Speed;
        }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("pipebee*.png", scaled: true, colorkey: true);
            _anim = new Animation<Texture2D>(imgs);
            CurrentImage = _anim.NextFrame();
        }

        public Rectangle SenseAndReturnHitbox(Hero hero)
        {
            if (!_turned && Rect.Y < hero.Rect.Y)
            {
                _turned = true;
                Movement.Y = 0;
                Movement.X = (Rect.CenterX() < hero.Rect.CenterX()) ? Speed : -Speed;
            }
            return Hitbox();
        }

        protected override (float x, float y) GetMove() => (Movement.X, Movement.Y);
        
        public override void Update()
        {
            var move = GetMove();
            if (!Env.IsMoveLegal(Hitbox(), (move.x, move.y))) Kill();
            else Rect = Rect.Move(move.x, move.y);
            
            SetCurrentImage();
            if (Env.IsOutsideMap(Hitbox())) Kill();
        }

        protected override void SetCurrentImage() => CurrentImage = _anim!.NextFrame();
    }

    // --- 6. BugPipe (Spawner) ---
    public class BugPipe : Character 
    {
        public override int StartingHp => int.MaxValue;
        public override bool IsInvulnerable => true;
        public override int Damage => 0;
        public override int Width => 48;
        public override int Height => 48;
        
        private int _cooldown;
        private (int c, int r) _spawnPos;

        public BugPipe(Environment env, (int c, int r) pos) : base(env, pos) 
        { 
            _spawnPos = pos;
        }
        
        protected override void InitImages() 
        {
            CurrentImage = new Texture2D { Width = 1, Height = 1 }; // Invisible placeholder
        }
        protected override void SetCurrentImage() { }
        protected override (float x, float y) GetMove() => (0,0);
        public override Rectangle Hitbox() => new Rectangle(0,0,0,0); // No collision
        public override void Draw() { } // Invisible

        public override void Update()
        {
            // Spawn logic
            if (Env.IsWorldPointVisible(Rect.X, Rect.Y))
            {
                _cooldown--;
                if (_cooldown <= 0)
                {
                    _cooldown = 120;
                    Spawn();
                }
            }
        }

        protected virtual void Spawn()
        {
            Env.EnemyGroup.Add(new PipeBug(Env, (_spawnPos.c, _spawnPos.r - 1)));
        }
    }
    
    // --- 7. Biter ---
    public class Biter : PipeBug
    {
        public override int StartingHp => 4;
        public override int Damage => 2;
        public override float Speed => 10f;

        public Biter(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("biter*.png", scaled: true, colorkey: true);
            _anim = new Animation<Texture2D>(imgs);
            CurrentImage = _anim.NextFrame();
        }
    }

    // --- 8. BiterPipe ---
    public class BiterPipe : BugPipe
    {
        public BiterPipe(Environment env, (int c, int r) pos) : base(env, pos) { }
        protected override void Spawn()
        {
            Env.EnemyGroup.Add(new Biter(Env, (RectForTileIndex().c, RectForTileIndex().r - 1)));
        }
        
        private (int c, int r) RectForTileIndex() => Env.TileIndexForPoint(Rect.X, Rect.Y);
    }

    // --- 9. Batzor ---
    public class Batzor : Character
    {
        public override int StartingHp => 3;
        public override int Damage => 1;
        public override float Speed => 5f;
        public override int Width => 48;
        public override int Height => 48;

        private Animation<Texture2D>? _fly;
        private Vector2 _vector = new Vector2(1, 1);
        private int _moveFrames = 40;
        private int _restFrames = 0;

        public Batzor(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("batzor*.png", scaled: true, colorkey: true);
            _fly = new Animation<Texture2D>(imgs);
            CurrentImage = _fly.NextFrame();
        }

        protected override (float x, float y) GetMove()
        {
            if (_moveFrames > 0)
            {
                Movement.X = _vector.X * Speed;
                Movement.Y = _vector.Y * Speed;
                _moveFrames--;
                if (_moveFrames == 0)
                    _restFrames = 20 + Raylib.GetRandomValue(0, 120);
            }
            else
            {
                Movement = Vector2.Zero;
                _restFrames--;
                if (_restFrames <= 0)
                {
                    _vector = new Vector2(-_vector.Y, _vector.X);
                    _moveFrames = 40;
                }
            }
            return (Movement.X, Movement.Y);
        }
        
        public override void Update()
        {
            var move = GetMove();
            if (!Env.IsMoveLegal(Hitbox(), (move.x, move.y))) Kill();
            else Rect = Rect.Move(move.x, move.y);
            
            SetCurrentImage();
            if (Env.IsOutsideMap(Hitbox())) Kill();
        }

        protected override void SetCurrentImage() => CurrentImage = _fly!.NextFrame();
    }

    // --- 10. Slug ---
    public class Slug : Character
    {
        public override int StartingHp => 6;
        public override int Damage => 10;
        public override float Speed => 1f;
        public override float Gravity => 2f;
        public override int Width => 96;
        public override int Height => 48;

        private Animation<Texture2D>? _crawlRight, _crawlLeft, _idleRight, _idleLeft;
        private int _moveFrames = 48;
        private int _restFrames = 0;

        public Slug(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var crawl = TextureCache.LoadImages("slug001*.png", scaled: true, colorkey: true)
                .Concat(TextureCache.LoadImages("slug002*.png", scaled: true, colorkey: true)).ToArray();
            _crawlLeft = new Animation<Texture2D>(crawl);
            _crawlRight = new Animation<Texture2D>(crawl.Select(TextureCache.FlipHorizontal).ToArray());
            
            var idle = TextureCache.LoadImages("slug000*.png", scaled: true, colorkey: true);
            _idleLeft = new Animation<Texture2D>(idle);
            _idleRight = new Animation<Texture2D>(idle.Select(TextureCache.FlipHorizontal).ToArray());
            
            CurrentImage = _idleLeft.NextFrame();
        }

        protected override (float x, float y) GetMove()
        {
            if (_moveFrames > 0)
            {
                _moveFrames--;
                if (_moveFrames == 0) _restFrames = 60 + Raylib.GetRandomValue(0, 60);
                return WalkBackAndForth();
            }
            else
            {
                Movement.X = 0;
                _restFrames--;
                if (_restFrames <= 0) _moveFrames = 48;
                return (0, Movement.Y);
            }
        }

        protected override void SetCurrentImage()
        {
             if (_moveFrames > 0)
                 CurrentImage = (Movement.X > 0 ? _crawlRight : _crawlLeft)!.NextFrame();
             else
                 CurrentImage = (Facing == Direction.Right ? _idleRight : _idleLeft)!.NextFrame();
        }
    }

    // --- 11. Baron (Boss) ---
    public class Baron : Character
    {
        public override int StartingHp => 100;
        public override int Damage => 3;
        public override float Speed => 6f;
        public override float Gravity => 2f;
        public override int Width => 384;
        public override int Height => 240;
        
        private Animation<Texture2D>? _walkRight, _walkLeft;
        private Texture2D _standRight, _standLeft;
        private int _moveFrames = 48;
        private int _restFrames = 0;
        private Vector2 _lastMove;
        
        private static Sound? _winSound;
        private static Sound WinSound => _winSound ??= Raylib.LoadSound("media/music/win.ogg");

        public Baron(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            // Helper to scale textures
            Texture2D Scale(Texture2D tex)
            {
                Image img = Raylib.LoadImageFromTexture(tex);
                Raylib.ImageResize(ref img, 384, 240); // Hardcoded boss size
                Texture2D scaled = Raylib.LoadTextureFromImage(img);
                Raylib.UnloadImage(img);
                return scaled;
            }

            // Load standard beaver textures then scale them
            var walkImgs = TextureCache.LoadImages("beaver1*.png", scaled: true, colorkey: true);
            var walkScaled = walkImgs.Select(Scale).ToArray();
            
            _walkRight = new Animation<Texture2D>(walkScaled);
            _walkLeft = new Animation<Texture2D>(walkScaled.Select(TextureCache.FlipHorizontal).ToArray());
            
            var standImg = TextureCache.LoadImage("beaver10000.png", scaled: true, colorkey: true);
            _standRight = Scale(standImg);
            _standLeft = TextureCache.FlipHorizontal(_standRight);
            
            CurrentImage = _standLeft;
        }

        protected override (float x, float y) GetMove()
        {
            float speed = 6f;
            int variableRest = 60;
            if (Hp < 10) { speed = 12f; variableRest = 30; }

            if (_moveFrames > 0)
            {
                _moveFrames--;
                if (_moveFrames == 0)
                {
                    _restFrames = 60 + Raylib.GetRandomValue(0, variableRest);
                    _lastMove = Movement;
                }
                
                // Override base Speed for WalkBackAndForth? 
                // WalkBackAndForth uses Accel/Speed. We can hack it by modifying Movement directly
                // or just implementing logic here.
                // Simple approach: set Movement.X
                if (Facing == Direction.Left) Movement.X = -speed;
                else Movement.X = speed;
                
                // Check wall collision manually
                int checkX = Facing == Direction.Left
                    ? (int)(Hitbox().Left() + Movement.X)
                    : (int)(Hitbox().Right() + Movement.X);
                var destTile = Env.TileIndexForPoint(checkX, Hitbox().Bottom());
                if (!Env.IsMoveLegal(Hitbox(), (Movement.X, Movement.Y))
                    || !Env.IsTileSupported(destTile.col, destTile.row))
                {
                    Facing = Facing == Direction.Left ? Direction.Right : Direction.Left;
                }
            }
            else
            {
                Movement.X = 0;
                _restFrames--;
                if (_restFrames <= 0)
                {
                    _moveFrames = 48;
                    Movement = _lastMove;
                }
            }

            return (Movement.X, Movement.Y);
        }

        protected override void SetCurrentImage()
        {
            if (Movement.X > 0) CurrentImage = _walkRight!.NextFrame();
            else if (Movement.X < 0) CurrentImage = _walkLeft!.NextFrame();
            else CurrentImage = Facing == Direction.Right ? _standRight : _standLeft;
        }

        public override void Die()
        {
             Raylib.PlaySound(WinSound);
             Env.DyingAnimationGroup.Add(new DyingAnimation(Rect, isBoss: true));
             Kill();
        }
    }
}
