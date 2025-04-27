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
        protected readonly int REQUIRED_QUANTITY_OF_PRODUCTS;
        /// <summary>
        /// Количество секунд на ожидание успешного парсинга
        /// </summary>
        protected readonly int SECONDS_BEFORE_TIMEOUT;
        public MarketParser(IConfiguration Configuration)
        {
            browser = new ChromiumWebBrowser();
            if (!int.TryParse(Configuration["Parsers:RequiredQuantityOfProducts"], out REQUIRED_QUANTITY_OF_PRODUCTS))
                REQUIRED_QUANTITY_OF_PRODUCTS = 50;
            if (!int.TryParse(Configuration["Parsers:SecondsBeforeTimeOut"], out SECONDS_BEFORE_TIMEOUT))
                SECONDS_BEFORE_TIMEOUT = 10;
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
