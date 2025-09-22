using System.Collections.Generic;
using GildedRoseKata;
using Xunit;

namespace GildedRoseTests;

public class GildedRoseTest
{
    // [Fact]
    public void Foo()
    {
        var items = new List<Item> { new() { Name = "foo", SellIn = 0, Quality = 0 } };
        var app = new GildedRose(items);
        app.UpdateQuality();
        Assert.Equal("fixme", items[0].Name);
    }
}