using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShopScoutWebApplication.Models;

namespace ShopScoutWebApplication.Controllers
{
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;
        private HttpClient httpClient;
        private const string APIURL = "http://127.0.0.1/api/v1";

        public SearchController(ILogger<SearchController> logger)
        {
            _logger = logger;
            httpClient = new HttpClient();
        }
        [Route("/search")]
        public async Task<IActionResult> Index()
        {
            return View();
        }
        public async Task<IActionResult> Result(string text, bool ozon, bool wb, Sort sort, int page, int count)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = "";
            string URL = APIURL + "/?text=" + text.Replace(" ", "+") + "&ozon=" + ozon + "&wb=" + wb + "&sort=" + sort + "&page=" + page + "&count=" + count;
            var response = await httpClient.GetAsync(URL);
            var results = await response.Content.ReadFromJsonAsync<APIV1Results>();

            ViewBag.APIV1Results = results;
            ViewBag.text = text;
            ViewBag.ozon = ozon;
            ViewBag.wb = wb;
            ViewBag.sort = sort;
            ViewBag.page = page;
            ViewBag.count = count;
            Response.Headers.CacheControl = "max-age=36000, public";
            Response.StatusCode = (int)response.StatusCode;
            return View();
        }
    }
}
