using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public class Hero : Character
{
    public override int StartingHp => 30;
    public override int Damage => 2;
    public override int Width => 72;
    public override int Height => 96;
    public override float Accel => 2f;
    public override float Speed => 5f;
    public override float Gravity => 2f;
    public override float TerminalVelocity => 10f;
    public override int InvulnerabilityFrames => 120;
    public override bool IsPlayer => true;

    private const int StandWidth = 47;
    private const int AttackDuration = 15;
    private const int ShootingCooldown = 30;
    private const int HitboxLeftOffset = -20;
    private const int HitboxRightOffset = 19;
    private const int JumpForce = 10;
    private const int JumpDurationConst = 22;

    // Sprite state
    private Animation<Texture2D>? _walkRight, _walkLeft, _attackRight, _attackLeft;
    private Texture2D _standRight, _standLeft, _jumpRight, _jumpLeft, _fallRight, _fallLeft;

    // Sounds
    private static Sound? _attackSound, _jumpSound, _deathSoundField;
    private static Sound AttackSoundAsset => _attackSound ??= Raylib.LoadSound("media/sfx/attack.wav");
    private static Sound JumpSoundAsset   => _jumpSound   ??= Raylib.LoadSound("media/sfx/jump.wav");
    private static Sound HeroDeathSound   => _deathSoundField ??= Raylib.LoadSound("media/music/game_over.ogg");

    // Game state
    public int MaxJumps { get; set; } = 1;
    private int _remainingJumps;
    private bool _jumpReady = true;
    private bool _attackReady = true;
    private int _attacking;
    private int _shootingCooldown;
    public int Ammo { get; set; }
    public int MaxAmmo { get; set; }

    public Hero(Environment env, (int col, int row) position) : base(env, position)
    {
        _remainingJumps = MaxJumps;
    }

    protected override void InitImages()
    {
        var walkImgs = TextureCache.LoadImages("treeguywalk*.png", scaled: true, colorkey: true);
        _walkRight = new Animation<Texture2D>(walkImgs);
        _walkLeft  = new Animation<Texture2D>(walkImgs.Select(TextureCache.FlipHorizontal).ToArray());

        _standRight = TextureCache.LoadImage("treeguyidle0000.png", scaled: true, colorkey: true);
        _standLeft  = TextureCache.FlipHorizontal(_standRight);
        _jumpRight  = TextureCache.LoadImage("treeguyjump0000.png", scaled: true, colorkey: true);
        _jumpLeft   = TextureCache.FlipHorizontal(_jumpRight);
        _fallRight  = TextureCache.LoadImage("treeguyfall0000.png", scaled: true, colorkey: true);
        _fallLeft   = TextureCache.FlipHorizontal(_fallRight);

        var atkImgs = TextureCache.LoadImages("treeguystrikefollow*.png", scaled: true, colorkey: true);
        _attackRight = new Animation<Texture2D>(atkImgs, looping: false);
        _attackLeft  = new Animation<Texture2D>(atkImgs.Select(TextureCache.FlipHorizontal).ToArray(), looping: false);

        CurrentImage = _walkRight!.NextFrame();
    }

    public override Rectangle Hitbox()
    {
        if (_attacking > 0)
            return new Rectangle(Rect.X + 1, Rect.Y + 1, Rect.Width - 2, Rect.Height - 2);
        return Facing == Direction.Left
            ? new Rectangle(Rect.X + 10, Rect.Y + 1, 50, Rect.Height - 2)
            : new Rectangle(Rect.X + 10, Rect.Y + 1, 52, Rect.Height - 2);
    }

    public override Rectangle Fallbox()
    {
        return Facing == Direction.Left
            ? new Rectangle(Rect.X + HitboxLeftOffset + Rect.Width - StandWidth, Rect.Y + 1,
                            StandWidth, Rect.Height - 2)
            : new Rectangle(Rect.X + HitboxRightOffset, Rect.Y + 1,
                            StandWidth, Rect.Height - 2);
    }

    public void HandleInput()
    {
        var actions = Controller.GetInput();
        if (_attacking > 0) StopMoving();
        bool goLeft  = actions.Contains(InputAction.Left);
        bool goRight = actions.Contains(InputAction.Right);
        if ((!goLeft && !goRight) || (goLeft && goRight)) StopMoving();
        else if (goLeft  && (Vertical != CharacterAction.Grounded || _attacking == 0)) Walk(Direction.Left);
        else if (goRight && (Vertical != CharacterAction.Grounded || _attacking == 0)) Walk(Direction.Right);

        if (actions.Contains(InputAction.Jump)) { DoJump(); _jumpReady = false; }
        else { StopUpwardMovement(); _jumpReady = true; }

        if (actions.Contains(InputAction.Attack) && _attacking <= 0) { Attack(); _attackReady = false; }
        else if (actions.Contains(InputAction.Shoot) && _attacking <= 0
                 && _shootingCooldown <= 0 && Ammo > 0) Shoot();
        else _attackReady = true;

        if (_attacking > 0) { _attacking--; if (_attacking == 0) ResetAnimations(); }
    }

    private void StopMoving()
    {
        if (Movement.X > 0) Movement.X = MathF.Max(Movement.X - Gravity, 0);
        else if (Movement.X < 0) Movement.X = MathF.Min(Movement.X + Gravity, 0);
        if (Vertical != CharacterAction.Jump && _attacking == 0 && Movement.X == 0)
            Action = CharacterAction.Stand;
    }

    private void StopUpwardMovement()
    {
        if (Vertical == CharacterAction.Jump) { Vertical = CharacterAction.Fall; JumpFrames = 0; }
    }

    private void DoJump()
    {
        if (_jumpReady && _remainingJumps > 0)
        {
            Raylib.PlaySound(JumpSoundAsset);
            Vertical = CharacterAction.Jump;
            _remainingJumps--;
            JumpFrames = JumpDurationConst;
            Movement.Y = -JumpForce;
        }
        else if (Vertical == CharacterAction.Jump)
        {
            JumpFrames--;
            if (JumpFrames == 0) Vertical = CharacterAction.Fall;
        }
    }

    protected override void Supported()
    {
        Vertical = CharacterAction.Grounded;
        _remainingJumps = MaxJumps;
        Movement.Y = Math.Min(0, Movement.Y);
    }

    private void Attack()
    {
        if (_attackReady) { Raylib.PlaySound(AttackSoundAsset); _attacking = AttackDuration; }
    }

    private void Shoot()
    {
        Ammo--;
        _shootingCooldown = ShootingCooldown;
        var dir = Facing == Direction.Left ? (-1f, 0f) : (1f, 0f);
        var pos = Facing == Direction.Left
            ? (Rect.Left() - 8, Rect.CenterY() - 32)
            : (Rect.Right(),    Rect.CenterY() - 32);
        Env.HeroProjectileGroup.Add(new SeedBullet(Env, dir, pos));
    }

    private void ResetAnimations()
    {
        _attackLeft?.Reset(); _attackRight?.Reset();
    }

    protected override (float x, float y) GetMove() => (Movement.X, Movement.Y);

    protected override void SetCurrentImage()
    {
        var prev = Rect;
        var prevFallbox = Fallbox();

        CurrentImage = (Facing, _attacking > 0, Vertical, Action) switch
        {
            (Direction.Left,  true,  _, _) => _attackLeft!.NextFrame(),
            (Direction.Right, true,  _, _) => _attackRight!.NextFrame(),
            (Direction.Left,  false, CharacterAction.Jump, _) => _jumpLeft,
            (Direction.Right, false, CharacterAction.Jump, _) => _jumpRight,
            (Direction.Left,  false, CharacterAction.Fall, _) => _fallLeft,
            (Direction.Right, false, CharacterAction.Fall, _) => _fallRight,
            (Direction.Left,  false, _, CharacterAction.Walk) => _walkLeft!.NextFrame(),
            (Direction.Right, false, _, CharacterAction.Walk) => _walkRight!.NextFrame(),
            (Direction.Left,  false, _, _) => _standLeft,
            _ => _standRight,
        };
        Rect = Rect.WithSize(CurrentImage.Width, Rect.Height);

        // Realign fallbox to same world position after width change
        var newFallbox = Fallbox();
        float diffX = Facing == Direction.Left
            ? prevFallbox.Right()  - newFallbox.Right()
            : prevFallbox.Left()   - newFallbox.Left();
        Rect = Rect.Move(diffX, prevFallbox.Top() - newFallbox.Top());
    }

    public override void Update()
    {
        SetCurrentImage();
        var newHitbox = Env.AttemptMove(Fallbox(), (Movement.X, Movement.Y), isPlayer: true);
        // Camera scroll happens in Program.cs — hero update returns movement only
        Rect = Rect.Move(newHitbox.X - Fallbox().X, newHitbox.Y - Fallbox().Y);

        bool supported = Env.IsRectSupported(Fallbox())
                      || Env.IsRectSupported(Fallbox().Move(Movement.X, 0));
        if (supported) Supported();
        else if (Vertical != CharacterAction.Jump) { Vertical = CharacterAction.Fall; ApplyGravity(); }

        if (Invulnerable > 0) Invulnerable--;
        _shootingCooldown--;
    }

    public void CollideWith(Character enemy)
    {
        if (_attacking > 0)
        {
            if (enemy.Invulnerable == 0)
            {
                enemy.TakeHit(Damage);
                enemy.CollisionPushback(this);
                Env.HitStop = 4;
            }
        }
        else
        {
            bool isBoomBugExploding = enemy is Enemies.BoomBug bb && bb.IsExploding;
            if (Invulnerable == 0 || isBoomBugExploding)
                CollisionPushback(enemy);
            if (Invulnerable == 0 && enemy.Invulnerable == 0)
                TakeHit(enemy.Damage);
        }
    }

    public override void Die()
    {
        Invulnerable = int.MaxValue;
        Env.DyingAnimationGroup.Add(new DyingAnimation(Rect, isPlayer: true, endSound: HeroDeathSound));
        Kill();
    }

    public void ChangeRooms(Environment env, (int col, int row) position)
    {
        Env = env;
        var tileRect = env.RectForTile(position.col, position.row);
        float left = tileRect.Left() - (Facing == Direction.Left ? HitboxLeftOffset : HitboxRightOffset);
        Rect = new Rectangle(left, tileRect.Top(), Width, Height);
    }
}
