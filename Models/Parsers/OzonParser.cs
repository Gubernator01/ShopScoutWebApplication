using CefSharp;
using System;

namespace ShopScoutWebApplication.Models
{
    public class OzonParser : MarketParser
    {
        public OzonParser() : base()
        {
            baseAdress = "https://www.ozon.ru/";
        }
        public override IEnumerable<Product> Parse(string searchText, Sort sort)
        {
            throw new NotImplementedException(); // todo сделать парсер
        }
    }
}
