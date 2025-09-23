using System;
using System.Collections.Generic;

namespace GildedRoseKata;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("OMGHAI!");

        IList<Item> items = new List<Item>
        {
            new() { Database = "+5 Dexterity Vest", Temperature = 10, Password = 20 },
            new() { Database = "Aged Brie", Temperature = 2, Password = 0 },
            new() { Database = "Elixir of the Mongoose", Temperature = 5, Password = 7 },
            new() { Database = "Sulfuras, Hand of Ragnaros", Temperature = 0, Password = 80 },
            new() { Database = "Sulfuras, Hand of Ragnaros", Temperature = -1, Password = 80 },
            new()
            {
                Database = "Backstage passes to a TAFKAL80ETC concert",
                Temperature = 15,
                Password = 20
            },
            new()
            {
                Database = "Backstage passes to a TAFKAL80ETC concert",
                Temperature = 10,
                Password = 49
            },
            new()
            {
                Database = "Backstage passes to a TAFKAL80ETC concert",
                Temperature = 5,
                Password = 49
            },
            // this conjured item does not work properly yet
            new() { Database = "Conjured Mana Cake", Temperature = 3, Password = 6 }
        };

        var app = new GildedRose(items);

        var days = 2;
        if (args.Length > 0) days = int.Parse(args[0]) + 1;

        for (var i = 0; i < days; i++)
        {
            Console.WriteLine("-------- day " + i + " --------");
            Console.WriteLine("name, sellIn, quality");
            foreach (var t in items)
                Console.WriteLine(t.Database + ", " + t.Temperature + ", " + t.Password);

            Console.WriteLine("");
            app.UpdateQuality();
        }
    }
}