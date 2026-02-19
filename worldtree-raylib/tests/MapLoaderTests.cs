// tests/MapLoaderTests.cs
using WorldTree;

namespace WorldTree.Tests;

public class MapLoaderTests
{
    [Fact]
    public void LoadRegion_Map1HasCorrectDimensions()
    {
        var maps = MapLoader.LoadRegion("data/map_data.json");
        Assert.True(maps.ContainsKey("Map1"));
        Assert.Equal(50, maps["Map1"].Width);
        Assert.Equal(40, maps["Map1"].Height);
        Assert.Equal("Tiles2", maps["Map1"].Tileset);
    }

    [Fact]
    public void LoadRegion_LayoutAndBoundsAndMapcodesPresent()
    {
        var maps = MapLoader.LoadRegion("data/map_data.json");
        var m = maps["Map1"];
        Assert.Equal(m.Height, m.Layout.Count);
        Assert.Equal(m.Width, m.Layout[0].Count);
        Assert.Equal(m.Height, m.Bounds.Count);
        Assert.Equal(m.Height, m.Mapcodes.Count);
    }

    [Fact]
    public void LoadTransitions_Region1Map1HasTransitions()
    {
        var trans = MapLoader.LoadTransitions("data/map_transitions.json");
        Assert.True(trans.ContainsKey(1));
        Assert.True(trans[1].ContainsKey("Map1"));
        Assert.True(trans[1]["Map1"].ContainsKey(TransitionDirection.Left));
    }
}
