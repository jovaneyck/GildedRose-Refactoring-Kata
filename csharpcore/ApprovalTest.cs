using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;

namespace csharpcore
{
    [UseReporter(typeof(XUnit2Reporter))]
    public class ApprovalTest
    {
        [Fact]
        public void ThirtyDays()
        {
            var fakeoutput = new StringBuilder();
            Console.SetOut(new StringWriter(fakeoutput));
            Console.SetIn(new StringReader("a\n"));

            Program.Main(new string[] { });
            var output = fakeoutput.ToString();

            Approvals.Verify(output);
        }

        [Fact]
        public void AllCombinations()
        {
            var items = new List<Item>();

            

            ApprovalTests.Combinations.CombinationApprovals.VerifyAllCombinations(
                RunItem,
                new [] { "Aged Brie", "Backstage passes to a TAFKAL80ETC concert", "Sulfuras, Hand of Ragnaros" },
                new[] { -1, 0, 1, 49, 50, 51 },
                new [] { -1, 0, 1, 5, 6, 7, 10, 11, 12 });
        }

        private string RunItem(string name, int quality, int sellIn)
        {
            var item = new Item{Name = name, Quality = quality, SellIn = sellIn};
            
            var app = new GildedRose(new List<Item> {item});
            app.UpdateQuality();

            return $"[{item.Name},{item.Quality},{item.SellIn}]";
        }
    }
}
