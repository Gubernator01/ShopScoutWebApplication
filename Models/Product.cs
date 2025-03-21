namespace ShopScoutWebApplication.Models
{
    /// <summary>
    /// Товар
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Цена
        /// </summary>
        public uint Price { get; set; }
        /// <summary>
        /// Количество оценок
        /// </summary>
        public uint ReviewsCount { get; set; }
        /// <summary>
        /// Рейтинг. Может находиться в диапазоне от 0 до 5.
        /// </summary>
        public float Rating
        {
            get { return Rating; }
            set
            {
                if (value > 5 || value < 0)
                {
                    throw new ArgumentException("Рейтинг должен быть в диапазоне от 0 до 5", nameof(Rating));
                }
                Rating = value;
            }
        }
        /// <summary>
        /// Название магазина
        /// </summary>
        public MarketName MarketName { get; set; }
        /// <summary>
        /// Ссылка на товар
        /// </summary>
        public string ProductURI { get; set; }
        /// <summary>
        /// Ссылка на изображение товара
        /// </summary>
        public string ProductImageURI { get; set; }
    }
}
