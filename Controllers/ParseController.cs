using Microsoft.AspNetCore.Mvc;
using ShopScoutWebApplication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopScoutWebApplication.Controllers
{
    /// <summary>
    /// Контроллер для парсеров и БД
    /// </summary>
    [NonController]
    public class ParseController
    {
        private ILogger logger;
        private IProductsDBController productsDBController;
        private List<MarketParser> marketParsers;
        private ParseController() { }
        public ParseController(ILoggerFactory loggerFactory, IProductsDBController productsDBController)
        {
            logger = loggerFactory.CreateLogger<ParseController>();
            this.productsDBController = productsDBController;
            marketParsers = [new OzonParser(), new WildberriesParser()];
        }
        /// <summary>
        /// Асинхронно получить список товаров из магазинов
        /// </summary>
        /// <param name="searchText">Текст поиска</param>
        /// <param name="sort">Способ сортировки</param>
        /// <param name="markets">Необходимые магазины</param>
        /// <returns>Список товаров</returns>
        public async Task<IEnumerable<Product>> ParseAsync(string searchText, Sort sort, MarketName[] markets)
        {
            var task = Task.Run(() => Parse(searchText, sort, markets));
            await task;
            return task.Result;
        }
        /// <summary>
        /// Получить список товаров из магазинов
        /// </summary>
        /// <param name="searchText">Текст поиска</param>
        /// <param name="sort">Способ сортировки</param>
        /// <param name="markets">Необходимые магазины</param>
        /// <returns>Список товаров</returns>
        public IEnumerable<Product> Parse(string searchText, Sort sort, MarketName[] markets)
        {
            List<Product> products = new List<Product>();
            if (markets == null || markets.Length == 0)
                return products;
            var uniqueMarkets = markets.Distinct();                                      // Проверка на наличие элементов в массиве магазинов для поиска, получение списка его уникальных элементов  

            var tasksFromDB = new List<Task<IEnumerable<Product>?>>();
            foreach (var market in uniqueMarkets)
            {
                tasksFromDB.Add(Task.Run(() => GetProductsFromDB(searchText, market, sort)));// Проверка наличия подобных результатов поиска для каждого магазина в базе
            }

            var marketsToStore = new List<MarketName>();
            var tasksFromParsers = new List<Task<IEnumerable<Product>>>();
            int marketIndex = 0;
            foreach (var market in uniqueMarkets)
            {
                var task = tasksFromDB[marketIndex];
                task.Wait();
                if (task.Result == null)                                                 // Если в базе не найдены товары, то требуется парсинг с сайтов
                {
                    marketsToStore.Add(market);
                    tasksFromParsers.Add(Task.Run(() => GetProductsFromParser(searchText, market, sort)));
                    logger.LogInformation($"Запущен парсинг \"{searchText}\" в магазине {market} сортировкой {sort}");
                }
                else                                                                     // Если в базе найдены товары, то они сохраняются в общий результат
                {
                    products.AddRange(task.Result);
                }
                marketIndex++;
            }

            for (int i = 0; i < tasksFromParsers.Count; i++)
            {
                var task = tasksFromParsers[i];
                var market = marketsToStore[i];
                task.Wait();
                products.AddRange(task.Result);                                          // Всё, что получено с парсеров сохраняется в общий результат и в базу
                Task.Run(() => PutProductsToDB(searchText, market, sort, task.Result));
            }

            return products;
        }
        private async Task<IProductsDBController> PutProductsToDB(string searchText, MarketName marketName, Sort sort, IEnumerable<Product> products)
        {
            var task = Task.Run(() => productsDBController.PutProducts(searchText, marketName, sort, products));
            await task;
            return task.Result;
        }
        private async Task<IEnumerable<Product>?> GetProductsFromDB(string searchText, MarketName marketName, Sort sort)
        {
            var task = Task.Run(() => productsDBController.GetProducts(searchText, marketName, sort));
            await task;
            return task.Result;
        }
        private async Task<IEnumerable<Product>> GetProductsFromParser(string searchText, MarketName marketName, Sort sort)
        {
            MarketParser? marketParser;
            switch (marketName)
            {
                case MarketName.Ozon:
                    marketParser = marketParsers.FirstOrDefault((m) => m is OzonParser);
                    break;
                case MarketName.Wildberries:
                    marketParser = marketParsers.FirstOrDefault((m) => m is WildberriesParser);
                    break;
                default:
                    throw new ArgumentException("Парсер для указанного магазина не определен", nameof(marketName));
            }
            if (marketParser == null)
                throw new ArgumentNullException("Доступного парсера для указанного магазина нет", nameof(marketParser));
            IEnumerable<Product> result;
            try
            {
                var task = Task.Run(() => marketParser.Parse(searchText, sort));
                await task;
                result = task.Result;
            }
            catch (Exception e)
            {
                logger.LogError($"Исключение: {e.Message}. При парсинге \"{searchText}\" в магазине {marketName} сортировкой {sort}");
                result = new List<Product>();
            }

            return result;
        }
    }
}
