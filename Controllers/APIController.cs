using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShopScoutWebApplication.Models;

namespace ShopScoutWebApplication.Controllers
{
    public class APIController : Controller
    {
        private readonly ILogger logger;
        private readonly IMemoryCache memoryCache;
        private readonly IProductSorter productSorter;
        private readonly IParseController parseController;
        private readonly int cacheExpirationTime;                                        // Время актуальности кэша, по умолчанию 30 минут
        public APIController(ILoggerFactory LoggerFactory, IMemoryCache MemoryCache, IProductSorter ProductSorter, IConfiguration Configuration, IParseController ParseController)
        {
            logger = LoggerFactory.CreateLogger<APIController>();
            memoryCache = MemoryCache;
            productSorter = ProductSorter;
            parseController = ParseController;
            if (!int.TryParse(Configuration["Cache:ExpirationTimeInMinutes"], out cacheExpirationTime))
                cacheExpirationTime = 30;
        }
        public async Task<IResult> v1(string text, bool ozon, bool wb, Sort sort, int page, int count)
        {
            List<Product> resultProducts = new List<Product>();                          // Результирующая коллекция
            if (count <= 0 || page < 0 || string.IsNullOrWhiteSpace(text))
            {
                logger.LogError($"Получен неверный запрос \"{text}, ozon:{ozon}, wb:{wb}, sort:{sort}\"");
                Response.StatusCode = 400;
                return Results.Json(new APIV1Results(resultProducts.Count, resultProducts));
            }
            List<Product> cacheProducts = new List<Product>();                           // Рабочая коллекция, должна быть закэширована
            int offset = page * count;                                                   // Смещение индекса коллекции с начала
            text = text.ToLower();
            string keyString = text + ozon + wb + sort;
            if (!memoryCache.TryGetValue(keyString, out cacheProducts))                  // Если в кэше не найден запрос, то он парсится, сортируется и кэшируется
            {
                logger.LogWarning($"Запрос \"{text}, ozon:{ozon}, wb:{wb}, sort:{sort}\" не найден в кэше");
                var markets = new List<MarketName>();
                if (ozon) markets.Add(MarketName.Ozon);
                if (wb) markets.Add(MarketName.Wildberries);
                cacheProducts = (await parseController.ParseAsync(text, sort, markets.ToArray())).ToList();
                cacheProducts = (await Task.Run(() => productSorter.Sort(cacheProducts, sort))).ToList();
                memoryCache.Set(keyString, cacheProducts, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationTime)
                });
            }

            if (offset < cacheProducts.Count)
            {
                int maxCount = cacheProducts.Count - offset;
                if (maxCount < count)
                    count = maxCount;
                resultProducts.AddRange(cacheProducts.GetRange(offset, count));          // В результирующую коллекцию товаров входят товары начиная с offset в количестве count или до конца
            }

            return Results.Json(new APIV1Results(cacheProducts.Count, resultProducts));  // API возвращает общее число товаров и результирующую коллекцию товаров
        }
    }
}
