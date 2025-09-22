using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GildedRoseKata;
using VerifyXunit;
using Xunit;

namespace GildedRoseTests;

public class ApprovalTest
{
    // [Fact]
    public Task Foo()
    {
        Item[] items = [new() { Name = "foo", SellIn = 0, Quality = 0 }];
        var app = new GildedRose(items);
        app.UpdateQuality();

        return Verifier.Verify(items);
    }

    [Fact]
    public Task ThirtyDays()
    {
        var fakeOutput = new StringBuilder();
        Console.SetOut(new StringWriter(fakeOutput));
        Console.SetIn(new StringReader($"a{Environment.NewLine}"));

        Program.Main(["30"]);
        var output = fakeOutput.ToString();

        return Verifier.Verify(output);
    }
}