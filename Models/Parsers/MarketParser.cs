using CefSharp.OffScreen;

namespace ShopScoutWebApplication.Models
{
    public abstract class MarketParser
    {
        protected ChromiumWebBrowser browser;
        protected string baseAddress = "";
        /// <summary>
        /// Достаточное количество товаров с магазина
        /// </summary>
        protected const int REQUIRED_QUANTITY_OF_PRODUCTS = 50;
        public MarketParser()
        {
            browser = new ChromiumWebBrowser();
        }
        /// <summary>
        /// Получить список товаров из этого магазина
        /// </summary>
        /// <param name="searchText">Текст поиска</param>
        /// <param name="sort">Способ сортировки</param>
        /// <returns>Список товаров</returns>
        public abstract IEnumerable<Product> Parse(string searchText, Sort sort);
    }
}
