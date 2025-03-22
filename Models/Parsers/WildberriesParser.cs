using CefSharp;

namespace ShopScoutWebApplication.Models
{
    public class WildberriesParser : MarketParser
    {
        public WildberriesParser() : base()
        {
            baseAdress = "https://www.wildberries.ru/";
        }
        public override IEnumerable<Product> Parse(string searchText, Sort sort)
        {
            throw new NotImplementedException(); // todo сделать парсер
        }
    }
}