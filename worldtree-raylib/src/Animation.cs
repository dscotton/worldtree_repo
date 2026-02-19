// src/Animation.cs
namespace WorldTree;

/// <summary>
/// Frame-based animation. Generic so it can be tested without Raylib.
/// Corresponds to worldtree/characters/animation.py.
/// </summary>
public class Animation<T>
{
    private readonly T[] _frames;
    private readonly bool _looping;
    private readonly int _frameDelay;
    private int _current;
    private int _frameCount;

    public Animation(T[] frames, int frameDelay = 2, bool looping = true)
    {
        _frames = frames;
        _frameDelay = frameDelay;
        _looping = looping;
        _current = 0;
        _frameCount = 0;
    }

    public T NextFrame()
    {
        T frame = _frames[_current];
        _frameCount++;
        if (_frameCount == _frameDelay)
        {
            _current++;
            _frameCount = 0;
            if (_current >= _frames.Length)
                _current = _looping ? 0 : _frames.Length - 1;
        }
        return frame;
    }

    public void Reset()
    {
        _current = 0;
        _frameCount = 0;
    }
}
