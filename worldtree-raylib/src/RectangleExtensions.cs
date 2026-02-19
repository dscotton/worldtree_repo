// src/RectangleExtensions.cs
using Raylib_cs;

namespace WorldTree;

public static class RectangleExtensions
{
    public static float Left(this Rectangle r) => r.X;
    public static float Right(this Rectangle r) => r.X + r.Width;
    public static float Top(this Rectangle r) => r.Y;
    public static float Bottom(this Rectangle r) => r.Y + r.Height;
    public static float CenterX(this Rectangle r) => r.X + r.Width / 2f;
    public static float CenterY(this Rectangle r) => r.Y + r.Height / 2f;

    public static Rectangle Move(this Rectangle r, float dx, float dy) =>
        new Rectangle(r.X + dx, r.Y + dy, r.Width, r.Height);

    public static Rectangle Move(this Rectangle r, (float x, float y) v) =>
        new Rectangle(r.X + v.x, r.Y + v.y, r.Width, r.Height);

    public static Rectangle WithSize(this Rectangle r, float w, float h) =>
        new Rectangle(r.X, r.Y, w, h);

    /// <summary>Returns true if this rectangle overlaps other.</summary>
    public static bool CollideRect(this Rectangle a, Rectangle b) =>
        Raylib.CheckCollisionRecs(a, b);
}
