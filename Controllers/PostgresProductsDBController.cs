using Microsoft.AspNetCore.Mvc;
using ShopScoutWebApplication.Models;
using ShopScoutWebApplication.Models.DBContext;

namespace ShopScoutWebApplication.Controllers
{
    /// <summary>
    /// Контроллер Postgres базы данных
    /// </summary>
    [NonController]
    public class PostgresProductsDBController : IProductsDBController
    {
        private ILogger logger;
        private string connectionString;
        private TimeSpan timeBeforeExpiration;

        private PostgresProductsDBController() { }
        public PostgresProductsDBController(ILoggerFactory loggerFactory, IConfiguration Configuration)
        {
            logger = loggerFactory.CreateLogger<PostgresProductsDBController>();
            connectionString = Configuration["PostgreSQL:ConnectionString"] ?? throw new ArgumentNullException("PostgreSQL:ConnectionString", "ConnectionString не определен");
            int minutesBeforeExpiration;
            if (!int.TryParse(Configuration["PostgreSQL:MinutesBeforeExpiration"], out minutesBeforeExpiration))
                minutesBeforeExpiration = 1440;                                                                    // По умолчанию сутки
            timeBeforeExpiration = TimeSpan.FromMinutes(minutesBeforeExpiration);
        }
        public IEnumerable<Product>? GetProducts(string searchText, MarketName marketName, Sort sort)
        {
            DeleteExpiredRequests();
            var products = new List<Product>();
            using (var db = new PostgresDBContext(connectionString))
            {
                var request = db.Requests.Where((r) => r.SearchText == searchText && r.Market == marketName && r.Sort == sort).FirstOrDefault();
                if (request == null)
                {
                    logger.LogInformation($"Товары по запросу {searchText}, {marketName}, {sort} не найдены в базе");
                    return null;
                }
                products.AddRange(db.Products.Where((p) => p.RequestId == request.Id));
            }
            logger.LogInformation($"Товары по запросу {searchText}, {marketName}, {sort} найдены в базе в количестве {products.Count}");
            return products;
        }
        public IProductsDBController PutProducts(string searchText, MarketName marketName, Sort sort, IEnumerable<Product> products)
        {
            DeleteExpiredRequests();
            var request = new Request() { Created = DateTime.Now.ToUniversalTime(), SearchText = searchText, Market = marketName, Sort = sort };
            var productsList = new List<ProductDB>();
            foreach (var product in products)
            {
                productsList.Add(new ProductDB(product, request));
            }

            using (var db = new PostgresDBContext(connectionString))
            {
                db.Requests.Add(request);
                db.Products.AddRange(productsList);
                db.SaveChanges();
                logger.LogInformation($"Товары по запросу {searchText}, {marketName}, {sort} сохранены в базе");
            }
            return this;
        }
        /// <summary>
        /// Удаление старых запросов
        /// </summary>
        private void DeleteExpiredRequests()
        {
            using (var db = new PostgresDBContext(connectionString))
            {
                var removedRequests = new List<Request>();
                foreach (var request in db.Requests)
                {
                    if (DateTime.Now.ToUniversalTime() - request.Created >= timeBeforeExpiration)
                    {
                        db.Products.ToList().RemoveAll((p) => p.RequestId == request.Id);
                        removedRequests.Add(request);
                    }
                }
                if (removedRequests.Count > 0)
                {
                    db.Requests.RemoveRange(removedRequests);
                    db.SaveChanges();
                    logger.LogInformation($"Из базы было удалено {removedRequests.Count} кэшированных запросов");
                }
            }
        }
    }
}