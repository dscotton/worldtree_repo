// src/characters/Projectile.cs (stub)
using Raylib_cs;
using System.Numerics;

namespace WorldTree;

public abstract class Projectile
{
    public Rectangle Rect;  // world coords
    public bool IsDead { get; protected set; }
    protected Environment Env;
    protected int _damage;
    protected Vector2 _movement;
    protected Texture2D _currentImage;

    protected Projectile(Environment env, int damage, float speed,
                         (float x, float y) direction, (float x, float y) position)
    {
        Env = env;
        _damage = damage;
        float mag = MathF.Sqrt(direction.x * direction.x + direction.y * direction.y);
        _movement = new Vector2(direction.x / mag * speed, direction.y / mag * speed);
        InitImages();
        Rect = new Rectangle(position.x, position.y, _currentImage.Width, _currentImage.Height);
    }

    protected abstract void InitImages();
    protected virtual void SetCurrentImage() { }

    public Rectangle Hitbox() => Rect;

    public virtual void CollideWith(Character sprite)
    {
        if (sprite.Invulnerable == 0)
            sprite.TakeHit(_damage);
    }

    public virtual void Update()
    {
        SetCurrentImage();
        if (!Env.IsMoveLegal(Rect, (_movement.X, _movement.Y)))
        { IsDead = true; return; }
        Rect = Rect.Move(_movement.X, _movement.Y);
    }

    public void Draw() =>
        Raylib.DrawTexture(_currentImage, (int)Rect.X, (int)Rect.Y, Color.White);
}

public class SeedBullet : Projectile
{
    private const int SeedDamage = 4;
    private const float SeedSpeed = 12f;
    private static Texture2D[]? _images;
    private Animation<Texture2D> _anim;

    private static Texture2D[] Images => _images ??=
        TextureCache.LoadImages("seedprojectile*.png", scaled: true, colorkey: true);

    public SeedBullet(Environment env, (float x, float y) direction, (float x, float y) position)
        : base(env, SeedDamage, SeedSpeed, direction, position) { }

    protected override void InitImages()
    {
        _anim = new Animation<Texture2D>(Images, frameDelay: 5);
        _currentImage = _anim.NextFrame();
    }

    protected override void SetCurrentImage() => _currentImage = _anim.NextFrame();
}

public class SporeCloud : Projectile
{
    private const int SporeDamage = 2;
    private const float SporeSpeed = 3f;
    private static Texture2D? _image;

    private static Texture2D Image => _image ??=
        TextureCache.LoadImage("spore0000.png", scaled: true, colorkey: true);

    public SporeCloud(Environment env, (float x, float y) direction, (float x, float y) position)
        : base(env, SporeDamage, SporeSpeed, direction, position)
    {
        Rect = Rect.Move(_movement.X * 5, _movement.Y * 15);
    }

    protected override void InitImages() => _currentImage = Image;
}
