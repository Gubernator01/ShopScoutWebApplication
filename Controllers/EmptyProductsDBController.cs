using Microsoft.AspNetCore.Mvc;
using ShopScoutWebApplication.Models;
using System.Collections.Generic;

namespace ShopScoutWebApplication.Controllers
{
    /// <summary>
    /// Контроллер базы, который ничего не делает, но создает видимость деятельности
    /// </summary>
    [NonController]
    public class EmptyProductsDBController : IProductsDBController
    {
        private ILogger logger;
        private EmptyProductsDBController() { }
        public EmptyProductsDBController(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<EmptyProductsDBController>();
        }
        public IEnumerable<Product>? GetProducts(string searchText, MarketName marketName, Sort sort)
        {
            logger.LogInformation($"Товары по запросу {searchText}, {marketName}, {sort} не найдены в базе");
            return null;
        }
        public IProductsDBController PutProducts(string searchText, MarketName marketName, Sort sort, IEnumerable<Product> products)
        {
            logger.LogInformation($"Товары по запросу {searchText}, {marketName}, {sort} не сохранены в базе");
            return this;
        }
    }
}
