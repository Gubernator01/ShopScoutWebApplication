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
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Цена
        /// </summary>
        public uint Price { get; set; }
        /// <summary>
        /// Количество оценок. Могут не существовать
        /// </summary>
        public uint? ReviewsCount { get; set; }
        private float? _rating;
        /// <summary>
        /// Рейтинг. Может находиться в диапазоне от 0 до 5. Может не существовать
        /// </summary>
        public float? Rating
        {
            get { return _rating; }
            set
            {
                if (value > 5 || value < 0)
                {
                    throw new ArgumentException("Рейтинг должен быть в диапазоне от 0 до 5", nameof(Rating));
                }
                _rating = value;
            }
        }
        /// <summary>
        /// Название магазина
        /// </summary>
        public MarketName MarketName { get; set; }
        /// <summary>
        /// Ссылка на товар
        /// </summary>
        public string ProductURI { get; set; } = string.Empty;
        /// <summary>
        /// Ссылка на изображение товара
        /// </summary>
        public string ProductImageURI { get; set; } = string.Empty;
    }
}
