using CefSharp;

namespace ShopScoutWebApplication.Models
{
    public class WildberriesParser : MarketParser
    {
        protected const int PRODUCTS_IN_ONE_PAGE = 100;
        public WildberriesParser() : base()
        {
            baseAddress = "https://www.wildberries.ru";
        }
        public override IEnumerable<Product> Parse(string searchText, Sort sort)
        {
            List<Product> products = new List<Product>();
            string preparedSearchText = searchText.Trim();
            preparedSearchText = preparedSearchText.Replace(' ', '+');
            preparedSearchText = "search=" + preparedSearchText;
            switch (sort)
            {
                case Sort.Popular:
                    preparedSearchText = "sort=popular&" + preparedSearchText;
                    break;
                case Sort.ByPriceDescending:
                    preparedSearchText = "sort=pricedown&" + preparedSearchText;
                    break;
                case Sort.ByPriceAscending:
                    preparedSearchText = "sort=priceup&" + preparedSearchText;
                    break;
                case Sort.ByRating:
                    preparedSearchText = "sort=rate&" + preparedSearchText;
                    break;
                default:
                    break;
            }
            var FirstPartOfURL = baseAddress + "/catalog/0/search.aspx?";

            try
            {
                var scrollScript = @"
                    (function(){
                        window.scrollBy({ top: 500, left: 0});
                        const cards = document.querySelectorAll('[data-card-index]');
                        return cards.length; 
                      })()";
                var parseScript = @"
                    (function(){
                        var result = [];
                        const cards = document.querySelectorAll('[data-card-index]');
                            for(let card of cards) {
                                let card_wrapper = card.querySelector('.product-card__wrapper');
                                let productURI = card_wrapper.querySelector('a.product-card__link');
                                let name = productURI.getAttribute('aria-label');
                                productURI = productURI.getAttribute('href');
                                let imageURI = card.querySelector('img');
                                imageURI = imageURI.getAttribute('src');
                                let price = card.querySelector('div.product-card__price');
                                price = price.querySelector('ins');
                                price = price.textContent;
                                let rating = card.querySelector('p.product-card__rating-wrap');
                                let reviews = rating.querySelector('span.product-card__count');
                                rating = rating.querySelector('span.address-rate-mini');
                                rating = rating.textContent;
                                reviews = reviews.textContent;
                                var product = {
                                    Name: name,
                                    Price: price,
                                    ReviewsCount: reviews,
                                    Rating: rating,
                                    ProductURI: productURI,
                                    ProductImageURI: imageURI,
                                };
                                result.push(product);
                            }
                        return result; 
                      })()";
                int page = 1;
                string URL;
                int productCount;
                int secondsCount = 0;
            newPage:
                if (page == 1)
                    URL = FirstPartOfURL + preparedSearchText;
                else
                    URL = FirstPartOfURL + "page=" + page + "&" + preparedSearchText;
                productCount = FirstLoadOfPage(URL);                                     // Ожидание прогрузки страницы
                if (productCount == 0) return products;
                int remainingProductCount = productCount - PRODUCTS_IN_ONE_PAGE * (page - 1);
                int expectedProductCount;
                if (remainingProductCount > PRODUCTS_IN_ONE_PAGE)
                    expectedProductCount = PRODUCTS_IN_ONE_PAGE;
                else
                    expectedProductCount = remainingProductCount;
                if (expectedProductCount <= 0) return products;
                parse:
                var scriptTask = browser.EvaluateScriptAsync(scrollScript);              // Проматывание страницы
                scriptTask.Wait();
                if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null)
                {
                    Task.Delay(1000).Wait();
                    if (secondsCount == 5)                                               // Совершается 5 попыток с паузой в секунду для загрузки страницы
                        throw new Exception("Неудачный парс");
                    secondsCount++;
                    goto parse;
                }
                var scrollScriptResponse = (int)scriptTask.Result.Result;

                if (scrollScriptResponse != expectedProductCount)
                {
                    Task.Delay(100).Wait();
                    goto parse;
                }

                scriptTask = browser.EvaluateScriptAsync(parseScript);
                scriptTask.Wait();
                if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null || ((List<dynamic>)scriptTask.Result.Result).Count == 0)
                {
                    Task.Delay(1000).Wait();
                    if (secondsCount == 5)
                        throw new Exception("Неудачный парс");
                    secondsCount++;
                    goto parse;
                }
                var parseScriptResponse = (List<dynamic>)scriptTask.Result.Result;

                List<string> exceptionalProductsURI = new List<string>();
                foreach (var rawProduct in parseScriptResponse)
                {
                    string Name = rawProduct.Name;
                    string ProductURI = rawProduct.ProductURI;
                    string ProductImageURI = rawProduct.ProductImageURI;
                    try                                                                  // Полученный список сырой информации парсится в необходимый формат
                    {
                        uint Price;
                        string PriceString = "";
                        foreach (var p in ((string)rawProduct.Price).Split([' ', '\u2009', '\u00A0'], StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (uint.TryParse(p, out uint _))
                                PriceString += p;
                        }
                        Price = uint.Parse(PriceString);

                        uint? ReviewsCount;
                        string ReviewsString = "";
                        if (rawProduct.ReviewsCount != null)
                            foreach (var p in ((string)rawProduct.ReviewsCount).Split([' ', '\u2009', '\u00A0'], StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (uint.TryParse(p, out uint _))
                                {
                                    ReviewsString += p;
                                }
                            }
                        if (uint.TryParse(ReviewsString, out uint reviewsCount))
                        {
                            ReviewsCount = reviewsCount;
                        }
                        else
                        {
                            ReviewsCount = null;
                        }

                        float? Rating;
                        string RatingString = "";
                        if (rawProduct.Rating != null)
                            RatingString = (string)rawProduct.Rating;
                        RatingString = RatingString.Replace('.', ',');
                        if (float.TryParse(RatingString, out float rating))
                        {
                            Rating = rating;
                        }
                        else
                        {
                            Rating = null;
                        }

                        var product = new Product { Name = Name, Price = Price, ReviewsCount = ReviewsCount, Rating = Rating, MarketName = MarketName.Wildberries, ProductURI = ProductURI, ProductImageURI = ProductImageURI };
                        products.Add(product);                                           // Итоговый товар
                    }
                    catch (Exception)
                    {
                        if (!exceptionalProductsURI.Contains(ProductURI))
                            exceptionalProductsURI.Add(ProductURI);
                        continue;
                    }
                }
                products = products                                                      // Необходимо проверить уникальность товаров
                .GroupBy(p => p.ProductURI)
                .Select(g => g.First())
                .ToList();

                if (products.Count < productCount - exceptionalProductsURI.Count && products.Count < REQUIRED_QUANTITY_OF_PRODUCTS)// Парcится пока есть что парсить и не достиг необходимого количества
                {
                    page++;
                    goto newPage;
                }
                return products;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private int FirstLoadOfPage(string URL)
        {
            browser.LoadUrlAsync(URL).Wait();

            int secondsCount = 0;
            var script = @"
                    (function(){
                        const searching_results = document.querySelector('span.searching-results__count');
                        let result = """";
                        if(searching_results != null) {
                            result = searching_results.textContent;
                            return result; 
                        }
                        return null;
                      })()";
        start:
            var scriptTask = browser.EvaluateScriptAsync(script);
            scriptTask.Wait();
            if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null)
            {
                Task.Delay(1000).Wait();
                if (secondsCount == 5)                                         // Совершается 5 попыток с паузой в секунду для загрузки страницы
                    throw new Exception("Неудачный парс");
                secondsCount++;
                goto start;
            }

            string taskResult = (string)scriptTask.Result.Result;
            var firstTaskResultSplited = taskResult.Split([' ', '\u2009'], '\u00A0');
            string productCountString = "";
            foreach (var item in firstTaskResultSplited)                       // Парс количества товаров в результате поиска
            {
                if (int.TryParse(item, out int _))
                {
                    productCountString += item;
                }
            }

            if (productCountString == "")
            {
                return 0;
            }
            var productCount = int.Parse(productCountString);                  // Доступное количество товаров

            return productCount;
        }
    }
}