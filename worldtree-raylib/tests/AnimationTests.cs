// tests/AnimationTests.cs
using WorldTree;

namespace WorldTree.Tests;

public class AnimationTests
{
    [Fact]
    public void LoopingAnimation_WrapsAround()
    {
        // framedelay=1: advances every call
        var anim = new Animation<int>([0, 1, 2], frameDelay: 1, looping: true);
        Assert.Equal(0, anim.NextFrame());
        Assert.Equal(1, anim.NextFrame());
        Assert.Equal(2, anim.NextFrame());
        Assert.Equal(0, anim.NextFrame()); // wraps
    }

    [Fact]
    public void NonLoopingAnimation_HoldsLastFrame()
    {
        var anim = new Animation<int>([0, 1, 2], frameDelay: 1, looping: false);
        Assert.Equal(0, anim.NextFrame());
        Assert.Equal(1, anim.NextFrame());
        Assert.Equal(2, anim.NextFrame());
        Assert.Equal(2, anim.NextFrame()); // holds
    }

    [Fact]
    public void FrameDelay_HoldsFrameForMultipleCalls()
    {
        var anim = new Animation<int>([0, 1], frameDelay: 2, looping: true);
        Assert.Equal(0, anim.NextFrame());
        Assert.Equal(0, anim.NextFrame()); // still on 0
        Assert.Equal(1, anim.NextFrame()); // advances on 3rd call
    }

    [Fact]
    public void Reset_GoesBackToStart()
    {
        var anim = new Animation<int>([0, 1, 2], frameDelay: 1, looping: false);
        anim.NextFrame(); anim.NextFrame(); anim.NextFrame(); // advance to end
        anim.Reset();
        Assert.Equal(0, anim.NextFrame());
    }
}
