using System.Collections.Generic;
using ApprovalTests.Reporters;
using Xunit;

namespace csharp
{
    public class CombinationApprovalsTest
    {
        [Fact]
        [UseReporter(typeof(DiffReporter))]
        public void Run()
        {
            ApprovalTests.Combinations.CombinationApprovals.VerifyAllCombinations(
                RunUpdateQualityFor,
                new List<string>{ "Aged Brie", "Backstage passes to a TAFKAL80ETC concert", "Sulfuras, Hand of Ragnaros", "my magical item" },
                new [] {-1,0,1,49,50,51},
                new[]{-1,0,1,5,6,7,10,11,12});
        }

        private static Item RunUpdateQualityFor(string name, int quality, int sellIn)
        {
            var item = new Item {Name = name, Quality = quality, SellIn = sellIn};
            var rose = new GildedRose(new List<Item> {item});
            rose.UpdateQuality();
            return item;
        }
    }
}