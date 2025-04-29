using Microsoft.AspNetCore.Mvc;
using ShopScoutWebApplication.Models;
using ShopScoutWebApplication.Models.DBContext;
using System.Collections.Generic;

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
            List<int> expiredRequestIDs = new List<int>();
            using (var db = new PostgresDBContext(connectionString))
            {
                expiredRequestIDs.AddRange(db.Requests.Where(r => (DateTime.Now.ToUniversalTime() - r.Created) >= timeBeforeExpiration).Select(r => r.Id));
            }
            using (var db = new PostgresDBContext(connectionString))
            {
                db.Products.RemoveRange(db.Products.Where(p => expiredRequestIDs.Contains(p.RequestId)));
                db.SaveChanges();
            }
            using (var db = new PostgresDBContext(connectionString))
            {
                db.Requests.RemoveRange(db.Requests.Where(r => expiredRequestIDs.Contains(r.Id)));
                db.SaveChanges();
            }
            if (expiredRequestIDs.Count > 0)
                logger.LogInformation($"Из базы было удалено {expiredRequestIDs.Count()} кэшированных запросов");
        }
    }
}