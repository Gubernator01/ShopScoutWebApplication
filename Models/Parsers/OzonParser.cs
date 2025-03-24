using CefSharp;
using CefSharp.DevTools.FedCm;
using System;

namespace ShopScoutWebApplication.Models
{
    public class OzonParser : MarketParser
    {
        public OzonParser() : base()
        {
            baseAddress = "https://www.ozon.ru";
        }
        public override IEnumerable<Product> Parse(string searchText, Sort sort)
        {
            List<Product> products = new List<Product>();
            string preparedSearchText = searchText.Trim();
            preparedSearchText = preparedSearchText.Replace(' ', '+');
            preparedSearchText = "text=" + preparedSearchText;
            switch (sort)
            {
                case Sort.Popular:
                    preparedSearchText = "sorting=score&" + preparedSearchText;
                    break;
                case Sort.ByPriceDescending:
                    preparedSearchText = "sorting=price_desc&" + preparedSearchText;
                    break;
                case Sort.ByPriceAscending:
                    preparedSearchText = "sorting=price&" + preparedSearchText;
                    break;
                case Sort.ByRating:
                    preparedSearchText = "sorting=rating&" + preparedSearchText;
                    break;
                default:
                    break;
            }
            var URL = baseAddress + "/search/?" + preparedSearchText;

            browser.LoadUrlAsync(URL).Wait();

            int secondsCount = 0;
            var script = @"
                    (function(){
                        const fulltextResultsHeader = document.querySelector('[data-widget=""fulltextResultsHeader""]');
                        const searchResultsError = document.querySelector('[data-widget=""searchResultsError""]');
                        let result = """";
                        if(fulltextResultsHeader != null) {
                            result = fulltextResultsHeader.childNodes[0].textContent;
                            return result; 
                        }
                        if(searchResultsError != null) {
                            result = searchResultsError.childNodes[2].childNodes[0].textContent;
                            return result; 
                        }
                        return null;
                      })()";
        parse1:
            var scriptTask = browser.EvaluateScriptAsync(script);
            scriptTask.Wait();
            if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null)
            {
                Task.Delay(1000).Wait();
                if (secondsCount == 5)                                         // Совершается 5 попыток с паузой в секунду для загрузки страницы
                    throw new Exception("Неудачный парс");
                secondsCount++;
                goto parse1;
            }

            string firstTaskResult = (string)scriptTask.Result.Result;
            var firstTaskResultSplited = firstTaskResult.Split([' ', '\u2009'], '\u00A0');
            string productCountString = "";
            foreach (var item in firstTaskResultSplited)                       // Парс количества товаров в результате поиска
            {
                if (int.TryParse(item, out int _))
                {
                    productCountString += item;
                }
            }

            if (productCountString == "")                                      // Если ничего нет, то возвращается пустой список
            {
                return products;
            }
            var productCount = int.Parse(productCountString);                  // Доступное количество товаров

            secondsCount = 0;
            List<string> exceptionalProductsURI = new List<string>();
            script = @"
                    (function(){
                        var result = [];
                            const contentScrollPaginator = document.getElementById('contentScrollPaginator');
                            let searchResultsV2s = contentScrollPaginator.querySelectorAll('[data-widget=""searchResultsV2""]');
                            for (let searchResultsV2 of searchResultsV2s)
                            {
                                let cards = searchResultsV2.querySelectorAll('[data-index]')
                                for(let card of cards) {
                                    let imageURI = card.childNodes[0];
                                    imageURI = imageURI.querySelector('img');
                                    imageURI = imageURI.getAttribute('src');
                                    let name = card.childNodes[2];
                                    let reviews = name.childNodes[name.childNodes.length - 3];
                                    name = name.querySelector('[href]');
                                    let productURN = name.getAttribute('href');
                                    name = name.textContent;
                                    let price = card.childNodes[2];
                                    price = price.childNodes[0];
                                    price = price.childNodes[0];
                                    price = price.childNodes[0];
                                    price = price.textContent;
                                    let rating;
                                    if(reviews.childNodes[0] != null){
                                        rating = reviews.childNodes[0];
                                        rating = rating.textContent;
                                    }
                                    if(reviews.childNodes[1] != null){
                                        reviews = reviews.childNodes[1];
                                        reviews = reviews.textContent;
                                    } else reviews = null;
                                    var product = {
                                        Name: name,
                                        Price: price,
                                        ReviewsCount: reviews,
                                        Rating: rating,
                                        ProductURN: productURN,
                                        ProductImageURI: imageURI,
                                    };
                                    result.push(product);
                                }
                            }
                        window.scrollBy({ top: 2000, left: 0});
                        return result; 
                      })()";
        parse2:
            scriptTask = browser.EvaluateScriptAsync(script);
            scriptTask.Wait();
            if (!(scriptTask.Result.Success) || scriptTask.Result.Result == null || ((List<dynamic>)scriptTask.Result.Result).Count == 0)
            {
                Task.Delay(1000).Wait();
                if (secondsCount == 5)                                         // Совершается 5 попыток с паузой в секунду для загрузки страницы
                    throw new Exception("Неудачный парс");
                secondsCount++;
                goto parse2;
            }
            var response = (List<dynamic>)scriptTask.Result.Result;

            foreach (var rawProduct in response)
            {
                string Name = rawProduct.Name;
                string ProductURI = baseAddress + rawProduct.ProductURN;
                string ProductImageURI = rawProduct.ProductImageURI;
                try                                                            // Полученный список сырой информации парсится в необходимый формат
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

                    var product = new Product { Name = Name, Price = Price, ReviewsCount = ReviewsCount, Rating = Rating, MarketName = MarketName.Ozon, ProductURI = ProductURI, ProductImageURI = ProductImageURI };
                    products.Add(product);                                     // Итоговый товар
                }
                catch (Exception)
                {
                    if (!exceptionalProductsURI.Contains(ProductURI))
                        exceptionalProductsURI.Add(ProductURI);
                    continue;
                }
            }
            products = products                                                // Необходимо проверить уникальность товаров
            .GroupBy(p => p.ProductURI)
            .Select(g => g.First())
            .ToList();
            if (products.Count < productCount - exceptionalProductsURI.Count && products.Count < REQUIRED_QUANTITY_OF_PRODUCTS)// Парится пока есть что парсить и не достиг необходимого количества
                goto parse2;
            return products;
        }
    }
}
