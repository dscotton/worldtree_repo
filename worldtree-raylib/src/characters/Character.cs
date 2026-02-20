// src/characters/Character.cs (stub)
using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public enum CharacterAction { Stand = 1, Walk = 2, Run = 3, Jump = 4, Fall = 5, Grounded = 6 }
public enum Direction { Left = 3, Right = 4 }

public interface IPushbackSource
{
    Rectangle Rect { get; }
    float PushBack { get; }
}

/// <summary>
/// Abstract base for all game characters (player, enemies).
/// All positions are in world (map) coordinates.
/// Corresponds to worldtree/characters/character.py.
/// </summary>
public abstract class Character : IPushbackSource
{
    // --- Constants (override in subclasses) ---
    public virtual int StartingHp => 1;
    public virtual bool IsInvulnerable => false;
    public virtual int InvulnerabilityFrames => 30;
    public virtual float Gravity => 2f;
    public virtual float TerminalVelocity => 10f;
    public virtual float Accel => 100f;
    public virtual float Speed => 0f;
    public virtual int JumpDuration => 0;
    public virtual int Width => 48;
    public virtual int Height => 48;
    public virtual float PushBack => 16f;
    public virtual int Damage => 0;
    public virtual bool IsPlayer => false;

    // --- State ---
    public Rectangle Rect { get; set; }       // world coordinates
    public bool IsDead { get; private set; }
    public int Hp { get; protected set; }
    public int MaxHp { get; protected set; }
    public int Invulnerable { get; protected set; }
    public CharacterAction Action { get; protected set; } = CharacterAction.Stand;
    public Direction Facing { get; protected set; } = Direction.Left;
    public CharacterAction Vertical { get; protected set; } = CharacterAction.Fall;
    public Environment Env { get; protected set; }
    protected Vector2 Movement;
    protected int JumpFrames;
    protected Texture2D CurrentImage;

    // Sounds (lazy-loaded after Raylib init)
    private static Sound? _hitSound;
    private static Sound? _deathSound;
    protected static Sound HitSound => _hitSound ??= Raylib.LoadSound("media/sfx/hit.wav");
    protected static Sound DeathSound => _deathSound ??= Raylib.LoadSound("media/sfx/death.wav");

    protected Character(Environment env, (int col, int row) position)
    {
        Env = env;
        Hp = MaxHp = StartingHp;
        var tileRect = env.RectForTile(position.col, position.row);
        // Align bottom of character to bottom of tile (matching Python behaviour)
        Rect = new Rectangle(tileRect.X, tileRect.Bottom() - Height, Width, Height);
        InitImages();
    }

    protected abstract void InitImages();
    protected abstract void SetCurrentImage();

    // Hitbox for collision — slightly inset from Rect (all in world coords)
    public virtual Rectangle Hitbox() =>
        new Rectangle(Rect.X + 1, Rect.Y + 1, Rect.Width - 2, Rect.Height - 2);

    // Fallbox for gravity — same as Hitbox by default
    public virtual Rectangle Fallbox() => Hitbox();

    // Draw the character (called inside BeginMode2D)
    public virtual void Draw()
    {
        float alpha = (Invulnerable > 0 && Invulnerable % 4 > 0) ? 128 : 255;
        var tint = new Color((byte)255, (byte)255, (byte)255, (byte)alpha);
        Raylib.DrawTexture(CurrentImage, (int)Rect.X, (int)Rect.Y, tint);
    }

    protected void Walk(Direction dir)
    {
        if (Action != CharacterAction.Jump) Action = CharacterAction.Walk;
        Facing = dir;
        if (dir == Direction.Left)
            Movement.X = Movement.X < -Speed ? Movement.X + Gravity : MathF.Max(Movement.X - Accel, -Speed);
        else
            Movement.X = Movement.X > Speed ? Movement.X - Gravity : MathF.Min(Movement.X + Accel, Speed);
    }

    protected void ApplyGravity()
    {
        if (Movement.Y < TerminalVelocity)
            Movement.Y = MathF.Min(Movement.Y + Gravity, TerminalVelocity);
    }

    protected virtual void Supported()
    {
        Vertical = CharacterAction.Grounded;
        Movement.Y = 0;
    }

    protected abstract (float x, float y) GetMove();

    public virtual void Update()
    {
        var move = GetMove();
        var newHitbox = Env.AttemptMove(Hitbox(), move, IsPlayer);
        // Reposition Rect so that Hitbox() matches newHitbox
        Rect = new Rectangle(newHitbox.X - 1, newHitbox.Y - 1,
                              Rect.Width, Rect.Height);
        if (Env.IsRectSupported(Fallbox()))
            Supported();
        else
            ApplyGravity();

        SetCurrentImage();
        if (Invulnerable > 0) Invulnerable--;
        if (Env.IsOutsideMap(Hitbox())) Kill();
    }

    public virtual void TakeHit(int damage)
    {
        Raylib.PlaySound(HitSound);
        Hp -= damage;
        if (Hp <= 0) Die();
        else Invulnerable = InvulnerabilityFrames;
    }

    public virtual void Die()
    {
        Raylib.PlaySound(DeathSound);
        DropItem();
        Env.DyingAnimationGroup.Add(new DyingAnimation(Rect));
        Kill();
    }

    // Drop table: each entry is a factory and its independent drop probability (0–99).
    // Subclasses override this to configure what they drop and how often.
    protected virtual (Func<Environment, (int col, int row), Powerup> Factory, int Probability)[] DropTable =>
        _defaultDropTable;

    private static readonly (Func<Environment, (int col, int row), Powerup> Factory, int Probability)[] _defaultDropTable =
    {
        ((env, pos) => new Powerups.HealthRestore(env, pos), 10),
        ((env, pos) => new Powerups.AmmoRestore(env, pos), 10),
    };

    protected virtual void DropItem()
    {
        var tile = Env.TileIndexForPoint(Hitbox().CenterX(), Hitbox().CenterY());
        foreach (var (factory, probability) in DropTable)
            if (Random.Shared.Next(100) < probability)
                Env.ItemGroup.Add(factory(Env, (tile.col, tile.row)));
    }

    public void Kill() => IsDead = true;

    public float GetDistance(Character other) =>
        Vector2.Distance(new Vector2(Hitbox().CenterX(), Hitbox().CenterY()),
                         new Vector2(other.Hitbox().CenterX(), other.Hitbox().CenterY()));

    public void CollisionPushback(IPushbackSource other)
    {
        float dx = Rect.CenterX() - other.Rect.CenterX();
        float dy = Rect.CenterY() - other.Rect.CenterY();
        float scalar = other.PushBack / MathF.Sqrt(dx * dx + dy * dy);
        Movement.X += (int)(dx * scalar);
        Movement.Y += (int)(dy * scalar);
    }

    protected (float x, float y) WalkBackAndForth()
    {
        Walk(Facing);
        int checkX = Facing == Direction.Left
            ? (int)(Hitbox().Left() + Movement.X)
            : (int)(Hitbox().Right() + Movement.X);
        var destTile = Env.TileIndexForPoint(checkX, Hitbox().Bottom());
        if (!Env.IsMoveLegal(Hitbox(), (Movement.X, Movement.Y))
            || !Env.IsTileSupported(destTile.col, destTile.row))
        {
            Facing = Facing == Direction.Left ? Direction.Right : Direction.Left;
        }
        return (Movement.X, Movement.Y);
    }

    public void RecoverHealth(int amount) => Hp = Math.Min(MaxHp, Hp + amount);
    public void RaiseMaxHp(int amount) { MaxHp += amount; RecoverHealth(amount); }
}

/// <summary>
/// Dying explosion animation. Corresponds to the Dying class in character.py.
/// </summary>
public class DyingAnimation
{
    private static Texture2D[]? _images;
    private Animation<Texture2D> _animation;
    public Rectangle Rect;
    public bool IsDead { get; private set; }
    private int _frames = 20;
    private bool _isPlayer;
    private bool _isBoss;
    private Sound? _endSound;

    private static Texture2D[] Images => _images ??=
        TextureCache.LoadImages("regularexplode1*.png", scaled: true,
                                colorkey: true);

    public DyingAnimation(Rectangle sourceRect, bool isPlayer = false,
                          bool isBoss = false, Sound? endSound = null)
    {
        _animation = new Animation<Texture2D>(Images, frameDelay: 3, looping: false);
        Rect = new Rectangle(0, 0, Images[0].Width, Images[0].Height);
        Rect.X = sourceRect.CenterX() - Rect.Width / 2f;
        Rect.Y = sourceRect.CenterY() - Rect.Height / 2f;
        _isPlayer = isPlayer;
        _isBoss = isBoss;
        _endSound = endSound;
        if (isPlayer || isBoss)
        {
            if (endSound.HasValue) Raylib.PlaySound(endSound.Value);
        }
    }

    public GameState Update()
    {
        _frames--;
        if (_frames <= 0)
        {
            IsDead = true;
            if (_isPlayer) return GameState.GameOver;
            if (_isBoss)   return GameState.Won;
        }
        return GameState.Playing;
    }

    public void Draw()
    {
        var tex = _animation.NextFrame();
        Raylib.DrawTexture(tex, (int)Rect.X, (int)Rect.Y, Color.White);
    }
}
