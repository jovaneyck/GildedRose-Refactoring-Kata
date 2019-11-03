using System.Linq;
using FsCheck;
using Xunit;

namespace csharp
{
    public class GoldenMasterTest
    {
        [Fact]
        public void Run()
        {
            var itemsGenerator = 
                Gen.ListOf(
                    from name in Gen.Elements("Aged Brie", "Backstage passes to a TAFKAL80ETC concert", "Sulfuras, Hand of Ragnaros", "my magical item")
                    from quality in Gen.Elements(-1, 0, 1, 49, 50, 51)
                    from sellIn in Gen.Elements(-1, 0, 1, 5, 6, 7, 10, 11, 12)
                    select new Item {Name = name, Quality = quality, SellIn = sellIn});

            var items = itemsGenerator
                .Eval(1000, Random.mkStdGen(1337))
                .OrderBy(i=>i.Name)
                .ThenBy(i=>i.Quality)
                .ThenBy(i=>i.SellIn)
                .ToList();

            var before = items.Select(i => i.ToString()).ToList();

            var rose = new GildedRose(items);
            rose.UpdateQuality();

            var after = items.Select(i => i.ToString()).ToList();

            var output = string.Join("\r\n", before.Zip(after, (b, a) => $"before: {b} - after: {a}"));
            ApprovalTests.Approvals.Verify(output);
        }
    }
}