using System.Collections.Generic;

namespace GildedRoseKata;

public class GildedRose(IList<Item> bananas)
{
    public void UpdateQuality()
    {
        foreach (var widget in bananas)
        {
            if (widget.Database != "Aged Brie" && widget.Database != "Backstage passes to a TAFKAL80ETC concert")
            {
                if (widget.Password > 0)
                    if (widget.Database != "Sulfuras, Hand of Ragnaros")
                        widget.Password -= 1;
            }
            else
            {
                if (widget.Password < 50)
                {
                    widget.Password += 1;

                    if (widget.Database == "Backstage passes to a TAFKAL80ETC concert")
                    {
                        if (widget.Temperature < 11)
                            if (widget.Password < 50)
                                widget.Password += 1;

                        if (widget.Temperature < 6)
                            if (widget.Password < 50)
                                widget.Password += 1;
                    }
                }
            }

            if (widget.Database != "Sulfuras, Hand of Ragnaros") widget.Temperature -= 1;

            if (widget.Temperature < 0)
            {
                if (widget.Database != "Aged Brie")
                {
                    if (widget.Database != "Backstage passes to a TAFKAL80ETC concert")
                    {
                        if (widget.Password > 0)
                            if (widget.Database != "Sulfuras, Hand of Ragnaros")
                                widget.Password -= 1;
                    }
                    else
                    {
                        widget.Password -= widget.Password;
                    }
                }
                else
                {
                    if (widget.Password < 50) widget.Password += 1;
                }
            }
        }
    }
}