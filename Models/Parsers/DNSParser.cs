
using CefSharp;
using CefSharp.DevTools.Page;

namespace ShopScoutWebApplication.Models.Parsers
{
    public class DNSParser : MarketParser
    {
        protected const int PRODUCTS_IN_ONE_PAGE = 18;
        public DNSParser(IConfiguration Configuration) : base(Configuration)
        {
            baseAddress = "https://www.dns-shop.ru";
        }

        public override IEnumerable<Product> Parse(string searchText, Sort sort)
        {
            List<Product> products = new List<Product>();
            string preparedSearchText = searchText.Trim();
            preparedSearchText = preparedSearchText.Replace(' ', '+');
            preparedSearchText = "q=" + preparedSearchText;
            switch (sort)
            {
                case Sort.Popular:
                    break;
                case Sort.ByPriceDescending:
                    preparedSearchText = "order=price-desc&" + preparedSearchText;
                    break;
                case Sort.ByPriceAscending:
                    preparedSearchText = "order=price-asc&" + preparedSearchText;
                    break;
                case Sort.ByRating:
                    preparedSearchText = "order=rating&" + preparedSearchText;
                    break;
                default:
                    break;
            }
            var FirstPartOfURL = baseAddress + "/search/?stock=now-today-tomorrow-later&";

            try
            {
                var parseScript = @"
                    (function(){
                        var result = [];
                        const cards = document.querySelectorAll('div.catalog-product');
                            for(let card of cards) {
                                let productURN = card.querySelector('a.catalog-product__name');
                                let name = productURN.textContent;
                                productURN = productURN.getAttribute('href');
                                let imageURI = card.querySelector('img');
                                imageURI = imageURI.getAttribute('data-src');
                                let price = card.querySelector('div.product-buy__price');
                                price = price.textContent;
                                let rating = card.querySelector('a.catalog-product__rating');
                                rating = rating.textContent;
                                var product = {
                                    Name: name,
                                    Price: price,
                                    ReviewsCount: rating,
                                    Rating: rating,
                                    ProductURN: productURN,
                                    ProductImageURI: imageURI,
                                };
                                result.push(product);
                            }
                        return result; 
                      })()";
                int page = 1;
                string URL;
                int productCount;
                int secondsCount;
            newPage:
                if (page == 1)
                    URL = FirstPartOfURL + preparedSearchText;
                else
                    URL = FirstPartOfURL + "p=" + page + "&" + preparedSearchText;
                productCount = FirstLoadOfPage(URL);                                     // Ожидание прогрузки страницы
                if (productCount == 0) return products;
                int remainingProductCount = productCount - PRODUCTS_IN_ONE_PAGE * (page - 1);
                int expectedProductCount;
                if (remainingProductCount > PRODUCTS_IN_ONE_PAGE || remainingProductCount > REQUIRED_QUANTITY_OF_PRODUCTS)
                {
                    if (REQUIRED_QUANTITY_OF_PRODUCTS < PRODUCTS_IN_ONE_PAGE)
                        expectedProductCount = REQUIRED_QUANTITY_OF_PRODUCTS;
                    else
                        expectedProductCount = PRODUCTS_IN_ONE_PAGE;
                }
                else
                    expectedProductCount = remainingProductCount;
                if (expectedProductCount <= 0) return products;
                secondsCount = 0;
            parse:
                var scriptTask = browser.EvaluateScriptAsync(parseScript);
                scriptTask.Wait();
                if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null || ((List<dynamic>)scriptTask.Result.Result).Count == 0)
                {
                    Task.Delay(1000).Wait();
                    if (secondsCount == SECONDS_BEFORE_TIMEOUT)
                        throw new Exception("Неудачный парс");
                    secondsCount++;
                    goto parse;
                }
                var parseScriptResponse = (List<dynamic>)scriptTask.Result.Result;

                List<string> exceptionalProductsURI = new List<string>();
                foreach (var rawProduct in parseScriptResponse)
                {
                    string Name = rawProduct.Name;
                    string ProductURI = baseAddress + rawProduct.ProductURN;
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
                            foreach (var p in ((string)rawProduct.ReviewsCount).Split('|', StringSplitOptions.RemoveEmptyEntries)[1].Split([' ', '\u2009', '\u00A0'], StringSplitOptions.RemoveEmptyEntries))
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
                            RatingString = ((string)rawProduct.Rating).Split('|', StringSplitOptions.RemoveEmptyEntries)[0].Split([' ', '\u2009', '\u00A0'], StringSplitOptions.RemoveEmptyEntries)[0];
                        RatingString = RatingString.Replace('.', ',');
                        if (float.TryParse(RatingString, out float rating))
                        {
                            Rating = rating;
                        }
                        else
                        {
                            Rating = null;
                        }
                        var product = new Product { Name = Name, Price = Price, ReviewsCount = ReviewsCount, Rating = Rating, MarketName = MarketName.DNS, ProductURI = ProductURI, ProductImageURI = ProductImageURI };
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
            }
            catch (Exception)
            {
                throw;
            }
            browser.Dispose();
            browser = new CefSharp.OffScreen.ChromiumWebBrowser();
            return products;
        }
        private int FirstLoadOfPage(string URL)
        {
            Task.WaitAny(browser.LoadUrlAsync(URL), Task.Delay(10000));

            int secondsCount = 0;
            var script = @"
                    (function(){
                        const not_found_results = document.querySelector('div.empty-search-results');
                        const searching_results = document.querySelector('span.products-count');
                        if(not_found_results != null) {
                            return false; 
                        }
                        if(searching_results != null) {
                            return true; 
                        }
                        return null;
                      })()";
        parse1:
            var scriptTask = browser.EvaluateScriptAsync(script);
            scriptTask.Wait();
            if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null)
            {
                Task.Delay(1000).Wait();
                if (secondsCount == SECONDS_BEFORE_TIMEOUT)                                      // Совершается несколько попыток с паузой в секунду для загрузки страницы
                    throw new Exception("Неудачный парс");
                secondsCount++;
                goto parse1;
            }
            bool resultsExist = (bool)scriptTask.Result.Result;
            if (!resultsExist) return 0;

            secondsCount = 0;
            script = @"
                    (function(){
                        const searching_results = document.querySelector('span.products-count');
                        let result = """";
                        if(searching_results != null) {
                            result = searching_results.textContent;
                            return result; 
                        }
                        return null;
                      })()";
        parse2:
            scriptTask = browser.EvaluateScriptAsync(script);
            scriptTask.Wait();
            if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null)
            {
                Task.Delay(1000).Wait();
                if (secondsCount == SECONDS_BEFORE_TIMEOUT)                                  // Совершается несколько попыток с паузой в секунду для загрузки страницы
                    throw new Exception("Неудачный парс");
                secondsCount++;
                goto parse2;
            }

            string taskResult = (string)scriptTask.Result.Result;
            var firstTaskResultSplited = taskResult.Split([' ', '\u2009', '\u00A0'], StringSplitOptions.RemoveEmptyEntries);
            string productCountString = "";
            foreach (var item in firstTaskResultSplited)                       // Парс количества товаров в результате поиска
            {
                if (int.TryParse(item, out int _))
                {
                    productCountString += item;
                }
                else
                {
                    break;
                }
            }

            if (productCountString == "" || productCountString == "0")
            {
                Task.Delay(1000).Wait();
                if (secondsCount == SECONDS_BEFORE_TIMEOUT)
                    throw new Exception("Неудачный парс");
                secondsCount++;
                goto parse2;
            }
            var productCount = int.Parse(productCountString);                  // Доступное количество товаров

            return productCount;
        }
    }
}
