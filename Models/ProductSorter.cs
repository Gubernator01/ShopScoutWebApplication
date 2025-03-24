
namespace ShopScoutWebApplication.Models
{
    /// <summary>
    /// Стандартный сортировщик
    /// </summary>
    public class ProductSorter : IProductSorter
    {
        public IEnumerable<Product> Sort(IEnumerable<Product> products, Sort sort)
        {
            IEnumerable<Product> result;

            switch (sort)
            {
                case ShopScoutWebApplication.Sort.Popular:
                    result = products.OrderByDescending((r) => r.ReviewsCount).ThenByDescending((r) => r.Rating).ThenBy((p) => p.Price);
                    break;
                case ShopScoutWebApplication.Sort.ByPriceDescending:
                    result = products.OrderByDescending((p) => p.Price);
                    break;
                case ShopScoutWebApplication.Sort.ByPriceAscending:
                    result = products.OrderBy((p) => p.Price);
                    break;
                case ShopScoutWebApplication.Sort.ByRating:
                    result = products.OrderByDescending((r) => r.Rating).ThenBy((p) => p.Price);
                    break;
                default:
                    throw new NotImplementedException("Указанной сортировки не найдено");
            }

            return result;
        }
    }
}
