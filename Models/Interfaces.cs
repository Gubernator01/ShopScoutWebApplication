using ShopScoutWebApplication.Models;
using System.Runtime.CompilerServices;

namespace ShopScoutWebApplication
{
    public interface IProductsDBController
    {
        /// <summary>
        /// Получить товары из базы
        /// </summary>
        /// <param name="searchText">Текст поиска</param>
        /// <param name="sort">Способ сортировки</param>
        /// <param name="marketName">Необходимый магазин</param>
        /// <returns>Список товаров или NULL, если подобного запроса в базе не найдено</returns>
        public IEnumerable<Product>? GetProducts(string searchText, MarketName marketName, Sort sort);
        /// <summary>
        /// Положить товары в базу
        /// </summary>
        /// <param name="searchText">Текст поиска</param>
        /// <param name="sort">Способ сортировки</param>
        /// <param name="marketName">Необходимый магазин</param>
        /// <param name="products">Список товаров</param>
        /// <returns>Этот же класс для сцепления</returns>
        public IProductsDBController PutProducts(string searchText, MarketName marketName, Sort sort, IEnumerable<Product> products);
    }
}
