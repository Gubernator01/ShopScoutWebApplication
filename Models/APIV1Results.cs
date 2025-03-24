namespace ShopScoutWebApplication.Models
{
    /// <summary>
    /// Контейнер ответа API V1
    /// </summary>
    public class APIV1Results
    {
        /// <summary>
        /// Всего найдено товаров
        /// </summary>
        public int Total {  get; set; }
        /// <summary>
        /// Возможно не полный список товаров
        /// </summary>
        public List<Product> Products { get; set; }
        public APIV1Results(int total, List<Product> products)
        {
            Total = total;
            Products = products;
        }
    }
}
