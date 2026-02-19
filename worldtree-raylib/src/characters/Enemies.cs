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

        public Beaver(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var walk = TextureCache.LoadImages("beaver1*.png", scaled: true, colorkey: true);
            _walkLeft = new Animation<Texture2D>(walk, frameDelay: 3);
            _walkRight = new Animation<Texture2D>(walk.Select(TextureCache.FlipHorizontal).ToArray(), frameDelay: 3);
            
            CurrentImage = _walkLeft.NextFrame();
        }

        protected override (float x, float y) GetMove()
        {
            WalkBackAndForth();
            return (Movement.X, Movement.Y);
        }

        protected override void SetCurrentImage()
        {
            if (Facing == Direction.Left) CurrentImage = _walkLeft!.NextFrame();
            else CurrentImage = _walkRight!.NextFrame();
        }
    }

    // --- 2. Dragonfly ---
    public class Dragonfly : Character
    {
        public override int StartingHp => 2;
        public override int Damage => 3;
        public override float Speed => 16f;
        public override float Gravity => 0f;
        public override int Width => 48;
        public override int Height => 48;

        private Animation<Texture2D>? _flyLeft, _flyRight;
        private Vector2 _vector = new Vector2(-1, 0);
        private int _moveFrames = 15;
        private int _restFrames = 0;

        public Dragonfly(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("dragonfly*.png", scaled: true, colorkey: true);
            _flyLeft = new Animation<Texture2D>(imgs);
            _flyRight = new Animation<Texture2D>(imgs.Select(TextureCache.FlipHorizontal).ToArray());
            CurrentImage = _flyLeft.NextFrame();
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

        protected override void SetCurrentImage()
        {
            CurrentImage = (_vector.X + _vector.Y < 0 ? _flyLeft : _flyRight)!.NextFrame();
        }
        
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
        public bool IsExploding => _exploding > 0;

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
            _triggeredRight = new Animation<Texture2D>(trig, frameDelay: 6, looping: false);
            _triggeredLeft = new Animation<Texture2D>(trig.Select(TextureCache.FlipHorizontal).ToArray(), frameDelay: 6, looping: false);

            var exp = TextureCache.LoadImages("bombexplode*.png", scaled: true, colorkey: true);
            _explode = new Animation<Texture2D>(exp, frameDelay: 4, looping: false);
            
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
                if (_triggered == 0) Explode();
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

        private void Explode()
        {
            Raylib.PlaySound(ExplodeSound);
            _exploding = 40;
            _explode!.Reset();
            
            // Resize hitbox to explosion size (maintain mid-bottom anchor)
            float oldCenterX = Rect.CenterX();
            float oldBottom = Rect.Bottom();
            
            // Explosion images are usually larger? Let's check size from _explode frames?
            // TextureCache doesn't expose frames easily.
            // Python: self.rect.width, self.rect.height = self.EXPLODING_IMAGES[0].get_size()
            // We'll assume typical explosion size or just use current image size in SetCurrentImage logic if handled there.
            // But Rect size matters for damage.
            // Let's assume explosion is roughly same size or bigger? 
            // In C# implementation here, CurrentImage updates Rect size in SetCurrentImage ONLY if we explicitly code it.
            // Character.SetCurrentImage resizes Rect to image size.
            // So we just need to ensure SetCurrentImage sets the explosion frame.
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
             if (_exploding > 0) Kill(); // Silent death if exploding
             else base.Die();
        }
    }

    // --- 4. Shooter ---
    public class Shooter : Character
    {
        public override int StartingHp => 4;
        public override int Damage => 1;
        public override int Width => 48;
        public override int Height => 96;
        
        private Animation<Texture2D>? _idle, _shootAnim;
        private Texture2D _staticImage;
        private int _cooldown;
        private Vector2 _aim = new Vector2(0, -1);

        public Shooter(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("mush*.png", scaled: true, colorkey: true);
            _staticImage = imgs[0]; // First frame is static?
            _idle = new Animation<Texture2D>(imgs, frameDelay: 4); // For backward compat if needed, but Python uses shoot anim
            _shootAnim = new Animation<Texture2D>(imgs, frameDelay: 4);
            CurrentImage = _staticImage;
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
                _shootAnim!.Reset();
            }
        }

        protected override void SetCurrentImage()
        {
            if (_cooldown > 12) CurrentImage = _staticImage;
            else CurrentImage = _shootAnim!.NextFrame();
        }
    }

    // --- 5. PipeBug ---
    public class PipeBug : Character
    {
        public override int StartingHp => 2;
        public override int Damage => 1;
        public override float Speed => 8f;
        public override float Gravity => 0f;
        public override int Width => 48;
        public override int Height => 48;

        protected Animation<Texture2D>? _animLeft, _animRight;
        private bool _turned;

        public PipeBug(Environment env, (int c, int r) pos) : base(env, pos)
        {
            Movement.Y = -Speed;
        }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("pipebee*.png", scaled: true, colorkey: true);
            _animLeft = new Animation<Texture2D>(imgs);
            _animRight = new Animation<Texture2D>(imgs.Select(TextureCache.FlipHorizontal).ToArray());
            CurrentImage = _animLeft.NextFrame();
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

        protected override void SetCurrentImage()
        {
            CurrentImage = (Movement.X > 0 ? _animRight : _animLeft)!.NextFrame();
        }
    }

    // --- 6. BugPipe (Spawner) ---
    public class BugPipe : Character 
    {
        public override int StartingHp => int.MaxValue;
        public override bool IsInvulnerable => true;
        public override int Damage => 0;
        public override int Width => 48;
        public override int Height => 48;
        
        private int _cooldown = 120;
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
            var imgs = TextureCache.LoadImages("biter1*.png", scaled: true, colorkey: true);
            _animLeft = new Animation<Texture2D>(imgs);
            _animRight = new Animation<Texture2D>(imgs.Select(TextureCache.FlipHorizontal).ToArray());
            CurrentImage = _animLeft.NextFrame();
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
        public override float Gravity => 0f;
        public override int Width => 48;
        public override int Height => 48;

        private Animation<Texture2D>? _fly;
        private Vector2 _vector = new Vector2(1, 1);
        private int _moveFrames = 40;
        private int _restFrames = 0;

        public Batzor(Environment env, (int c, int r) pos) : base(env, pos) { }

        protected override void InitImages()
        {
            var imgs = TextureCache.LoadImages("batzor1*.png", scaled: true, colorkey: true);
            _fly = new Animation<Texture2D>(imgs);
            CurrentImage = _fly.NextFrame();
        }

        public override Rectangle Hitbox() =>
            new Rectangle(Rect.X + 1, Rect.Y + 1, Rect.Width - 2, Rect.Height - 24);

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
            _crawlLeft = new Animation<Texture2D>(crawl, frameDelay: 4);
            _crawlRight = new Animation<Texture2D>(crawl.Select(TextureCache.FlipHorizontal).ToArray(), frameDelay: 4);
            
            var idle = TextureCache.LoadImages("slug000*.png", scaled: true, colorkey: true);
            _idleLeft = new Animation<Texture2D>(idle, frameDelay: 4);
            _idleRight = new Animation<Texture2D>(idle.Select(TextureCache.FlipHorizontal).ToArray(), frameDelay: 4);
            
            CurrentImage = _idleLeft.NextFrame();
        }

        public override Rectangle Hitbox()
        {
            if (Facing == Direction.Left)
                return new Rectangle(Rect.X + 1, Rect.Y + 1, 46, 46);
            else
                return new Rectangle(Rect.X + 1 + (Width - 46), Rect.Y + 1, 46, 46);
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
            
            _walkRight = new Animation<Texture2D>(walkScaled, frameDelay: 3);
            _walkLeft = new Animation<Texture2D>(walkScaled.Select(TextureCache.FlipHorizontal).ToArray(), frameDelay: 3);
            
            CurrentImage = _walkLeft.NextFrame();
        }

        protected override (float x, float y) GetMove()
        {
            float speed = 6f;
            int restBase = 60;
            int variableRest = 60;
            if (Hp < 10) { speed = 12f; restBase = 0; variableRest = 30; }

            if (_moveFrames > 0)
            {
                _moveFrames--;
                if (_moveFrames == 0)
                {
                    _restFrames = restBase + Raylib.GetRandomValue(0, variableRest);
                    _lastMove = Movement;
                }
                
                if (Facing == Direction.Left) Movement.X = -speed;
                else Movement.X = speed;
                
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
            if (Facing == Direction.Left) CurrentImage = _walkLeft!.NextFrame();
            else CurrentImage = _walkRight!.NextFrame();
        }

        public override void Die()
        {
             Raylib.PlaySound(WinSound);
             // TODO: Scale explosion? Or add custom DyingAnimation logic?
             // Fix 13d suggests passing scaled images or custom DyingAnimation.
             // We can just rely on standard one for now or pass scaled images if we loaded them.
             // But we didn't load scaled explosion images.
             // Let's stick to standard behavior unless we want to implement scaling logic in Die.
             Env.DyingAnimationGroup.Add(new DyingAnimation(Rect, isBoss: true));
             Kill();
        }
    }
}
