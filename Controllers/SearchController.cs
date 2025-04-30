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
        private int maxRequestsCount;

        public SearchController(ILogger<SearchController> logger, IConfiguration Configuration)
        {
            _logger = logger;
            httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(3) };
            if (!int.TryParse(Configuration["Search:MaxRequests"], out maxRequestsCount))
                maxRequestsCount = 1;
        }
        [Route("/search")]
        public async Task<IActionResult> Index()
        {
            return View();
        }
        public async Task<IActionResult> Result(string text, bool ozon, bool wb, bool dns, Sort sort, int page, int count)
        {
            if (GlobalVariables.SearchRequestsCount >= maxRequestsCount)
                return View("Deny");
            if (string.IsNullOrWhiteSpace(text))
                text = "";
            string URL = APIURL + "/?text=" + text.Replace(" ", "+") + "&ozon=" + ozon + "&wb=" + wb + "&dns=" + dns + "&sort=" + sort + "&page=" + page + "&count=" + count;
            HttpResponseMessage? response = null;
            APIV1Results? results;
            GlobalVariables.SearchRequestsCount++;
            try
            {
                response = await httpClient.GetAsync(URL);
                results = await response.Content.ReadFromJsonAsync<APIV1Results>();
            }
            catch (Exception)
            {
                _logger.LogError("Таймаут");
                results = null;
            }
            GlobalVariables.SearchRequestsCount--;

            ViewBag.APIV1Results = results;
            ViewBag.text = text;
            ViewBag.ozon = ozon;
            ViewBag.wb = wb;
            ViewBag.dns = dns;
            ViewBag.sort = sort;
            ViewBag.page = page;
            ViewBag.count = count;
            Response.Headers.CacheControl = "max-age=36000, public";
            if (response != null)
                Response.StatusCode = (int)response.StatusCode;
            else
            {
                Response.StatusCode = 524;
                View("ServerError5xx", 524);
            }
            return View();
        }
    }
}
