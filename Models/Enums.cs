namespace ShopScoutWebApplication
{
    /// <summary>
    /// Магазин
    /// </summary>
    public enum MarketName
    {
        Ozon = 0,
        Wildberries = 1
    }
    /// <summary>
    /// Способ сортировки
    /// </summary>
    public enum Sort
    {
        /// <summary>
        /// По популярности
        /// </summary>
        Popular = 0,
        /// <summary>
        /// По убыванию цены
        /// </summary>
        ByPriceDescending = 1,
        /// <summary>
        /// По возрастанию цены
        /// </summary>
        ByPriceAscending = 2,
        /// <summary>
        /// По рейтингу
        /// </summary>
        ByRating = 3
    }
}
