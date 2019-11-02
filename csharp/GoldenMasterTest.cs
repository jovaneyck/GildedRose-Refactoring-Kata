using System.Collections.Generic;
using System.Linq;
using ApprovalTests.Reporters;
using FsCheck;
using Xunit;

namespace csharp
{
    public class GoldenMasterTest
    {
        [Fact]
        [UseReporter(typeof(DiffReporter))]
        public void Run()
        {
            var nameGenerator = Gen.Elements("Aged Brie", "Backstage passes to a TAFKAL80ETC concert", "Sulfuras, Hand of Ragnaros", "my magical item");
            var numberGenerator = Gen.Choose(-50, 100);

            var itemGenerator =
                from name in nameGenerator
                from quality in numberGenerator
                from sellIn in numberGenerator
                select new Item {Name = name, Quality = quality, SellIn = sellIn};

            var itemsGenerator = Gen.ListOf(itemGenerator);

            var items = itemsGenerator.Eval(1000, Random.mkStdGen(1337)).ToList();

            var before = items.Select(i => i.ToString()).ToList();

            var rose = new GildedRose(items);
            rose.UpdateQuality();

            var after = items.Select(i => i.ToString()).ToList();

            var output = string.Join("\r\n", before.Zip(after, (b, a) => $"before: {b} - after: {a}"));
            ApprovalTests.Approvals.Verify(output);
        }
    }
}