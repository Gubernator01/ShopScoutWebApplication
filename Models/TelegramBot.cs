using ShopScoutWebApplication.Controllers;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ShopScoutWebApplication.Models
{
    /// <summary>
    /// Телеграмм бот
    /// </summary>
    public class TelegramBot
    {
        private TelegramBotClient bot;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private ILogger<TelegramBot> logger;
        private string token;
        private int countOfMessages;
        private int refreshTimeInSeconds;
        private int maxCountOfProductsPerRequest;
        private HttpClient httpClient;
        private const string APIURL = "http://127.0.0.1/api/v1";
        private int maxRequestsCount;
        private const int STAGES_COUNT = 4;                                           // Не убирать из массивов
        private static readonly string[] messageTexts = new string[STAGES_COUNT]{
            "Осуществлять поиск в OZON?",
            "Осуществлять поиск в Wildberries?",
            "Осуществлять поиск в DNS?",
            "Выберите способ сортировки:"
        };
        private static readonly string[] queryTextsParts = new string[STAGES_COUNT + 1]{
            "OZON: ",
            "Wildberries: ",
            "DNS: ",
            "Сортировка: ",
            "Страница: "
        };
        private static readonly string[] keyboardTextYesNo = {
            "Да",
            "Нет"
        };
        private static readonly string[] keyboardTextNextBack = {
            "Далее",
            "Назад"
        };
        private static readonly string[] keyboardTextSort = {
            "По популярности",
            "По убыванию цены",
            "По возрастанию цены",
            "По рейтингу"
        };
        private readonly InlineKeyboardMarkup[] keyboardsForming = {
            new(
            new[] {
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextYesNo[0]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextYesNo[1]) }
            }),
            new(
            new[] {
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextYesNo[0]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextYesNo[1]) }
            }),
            new(
            new[] {
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextYesNo[0]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextYesNo[1]) }
            }),
            new(
            new[] {
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextSort[0]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextSort[1]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextSort[2]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextSort[3]) }
            })
        };
        private readonly InlineKeyboardMarkup[] keyboardsSearching = {
            new(
            new[] {
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextNextBack[0]) },
                new[] { InlineKeyboardButton.WithCallbackData(keyboardTextNextBack[1]) }
            })
        };

        private TelegramBot() { }
        public TelegramBot(ILoggerFactory loggerFactory, IConfiguration Configuration)
        {
            logger = loggerFactory.CreateLogger<TelegramBot>();
            token = Configuration["TelegramBot:Token"] ?? throw new ArgumentNullException("TelegramBot:Token", "TelegramBot:Token не определен");
            bot = new TelegramBotClient(token);
            if (!int.TryParse(Configuration["TelegramBot:CountOfMessages"], out countOfMessages))
                countOfMessages = 30;
            if (!int.TryParse(Configuration["TelegramBot:RefreshTimeInSeconds"], out refreshTimeInSeconds))
                refreshTimeInSeconds = 7;
            if (!int.TryParse(Configuration["TelegramBot:MaxCountOfProductsPerRequest"], out maxCountOfProductsPerRequest))
                maxCountOfProductsPerRequest = 9;
            if (!int.TryParse(Configuration["Search:MaxRequests"], out maxRequestsCount))
                maxRequestsCount = 1;
            httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(3) };
        }
        ~TelegramBot()
        {
            Stop();
        }
        public void Start()
        {
            bot.StartReceiving(
                updateHandler: HandleUpdate,               // Пока один HandleUpdate отрабатывает 2 не запустится
                errorHandler: HandleError,
                cancellationToken: cts.Token
                );
        }
        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="id">ID пользователя</param>
        /// <param name="str">Сообщение</param>
        public async Task Message(long userId, string str)
        {
            await bot.SendMessage(userId, str);
        }
        /// <summary>
        /// Остановить бота (ОБЯЗАТЕЛЬНО ВЫЗЫВАТЬ ПРИ ЗАВЕРШЕНИИ РАБОТЫ)
        /// </summary>
        public void Stop()
        {
            cts.Cancel();
        }
        /// <summary>
        /// Обработка обновления
        /// </summary>
        private async Task HandleUpdate(ITelegramBotClient _, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                switch (update.Type)
                {
                    case UpdateType.Message:                                 // Простое сообщение
                        HandleMessage(update.Message!);
                        break;
                    case UpdateType.CallbackQuery:                           // Кнопка
                        HandleButton(update.CallbackQuery!);
                        break;
                }
            });
        }
        /// <summary>
        /// Обработка ошибки
        /// </summary>
        private async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception.Message);
        }
        /// <summary>
        /// Обработка сообщения
        /// </summary>
        /// <param name="msg">сообщение</param>
        private async Task HandleMessage(Message msg)
        {
            var user = msg.From;
            var text = msg.Text ?? string.Empty;
            if (user is null)
                return;
            logger.LogInformation($"{user.FirstName} {user.LastName}({user.Username}, ID:{user.Id}) написал {text}");
            if (text.StartsWith('/'))
                await HandleCommand(user.Id, text);
        }
        /// <summary>
        /// Обработка команды
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="command">команда</param>
        private async Task HandleCommand(long userId, string command)
        {
            if (command == "/start")
                await StartMessage(userId);
            if (command.StartsWith("/search"))
            {
                string text;
                try
                {
                    text = command.Substring(8);
                }
                catch (ArgumentOutOfRangeException)
                {
                    text = string.Empty;
                }
                if (string.IsNullOrWhiteSpace(text))
                {
                    await Message(userId, "Строка поиска пустая, поиск не состоялся");
                    return;
                }
                await SendMenu(userId, text + "\n\n" + messageTexts[0], keyboardsForming[0]);
            }
        }
        /// <summary>
        /// Отправить стартовое сообщение
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns></returns>
        private async Task StartMessage(long userId)
        {
            await Message(userId, "Для поиска введите команду в формате:\n/search {ваш запрос для поиска}");
        }
        /// <summary>
        /// Отправить меню
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="queryText">Текст меню</param>
        /// <param name="keyboard">Клавиатура</param>
        private async Task SendMenu(long userId, string queryText, InlineKeyboardMarkup keyboard)
        {
            await bot.SendMessage(
                userId,
                queryText,
                ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        /// <summary>
        /// Изменить меню
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="queryText">Текст меню</param>
        /// <param name="keyboard">Клавиатура</param>
        /// <param name="messageId">ID сообщения</param>
        private async Task EditMenu(long userId, string queryText, InlineKeyboardMarkup keyboard, int messageId)
        {
            await bot.EditMessageText(
                userId,
                messageId,
                queryText,
                ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        /// <summary>
        /// Обработка нажатия кнопки
        /// </summary>
        /// <param name="query">запрос</param>
        private async Task HandleButton(CallbackQuery query)
        {
            await bot.AnswerCallbackQuery(query.Id);
            string queryText = query.Message.Text;
            string[] queryTextSplitted = queryText.Split('\n', StringSplitOptions.None);
            bool formingInProgress = false;
            foreach (var item in queryTextSplitted)                                      // Условие проверки формирования запроса
            {
                if (string.IsNullOrWhiteSpace(item)) formingInProgress = true;
            }
            if (formingInProgress)
            {
                await FormingQuery(query);
                return;
            }
            else                                                                         // Запрос сформирован
            {
                await SearchQuery(query);
                return;
            }
        }
        /// <summary>
        /// Процесс формирования запроса к поисковику
        /// </summary>
        /// <param name="query">запрос к боту</param>
        private async Task FormingQuery(CallbackQuery query)
        {
            string queryText = query.Message.Text;
            string[] queryTextSplitted = queryText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            queryText = string.Empty;
            for (int i = 0; i < queryTextSplitted.Length - 1; i++)
            {
                queryText += queryTextSplitted[i] + '\n';
            }
            queryText = queryText.Substring(0, queryText.Length - 1);
            var s = queryText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int stage = s.Length - 1;
            queryText += '\n' + queryTextsParts[stage] + query.Data;
            stage++;

            if (stage < STAGES_COUNT)                                                               // Если не дошли до последней стадии формирования запроса - формируем
            {
                await EditMenu(query.Message!.Chat.Id, queryText + "\n\n" + messageTexts[stage], keyboardsForming[stage], query.Message.MessageId);
            }
            else                                                                                    // Сформировано
            {
                query.Message.Text = queryText + "\n" + queryTextsParts[STAGES_COUNT] + "1";
                await SearchQuery(query);                                                           // Первый вызов поиска после формирования запроса
                return;
            }
        }
        /// <summary>
        /// Выполнение запроса к поисковику
        /// </summary>
        /// <param name="query">запрос к боту</param>
        private async Task SearchQuery(CallbackQuery query)
        {
            if (GlobalVariables.SearchRequestsCount >= maxRequestsCount)
            {
                await Message(query.Message!.Chat.Id, "Сервер перегружен, повторите попытку позже");
                return;
            }
            // Парсинг параметров из сообщения
            string text = string.Empty;
            bool ozon = false;
            bool wb = false;
            bool dns = false;
            Sort sort = Sort.Popular;
            int page = 0;

            var textSplitted = query.Message.Text.Split('\n');
            text = textSplitted[0];
            string temp;
            temp = textSplitted[1].Substring(queryTextsParts[0].Length);
            if (temp == keyboardTextYesNo[0])
                ozon = true;
            else
                ozon = false;
            temp = textSplitted[2].Substring(queryTextsParts[1].Length);
            if (temp == keyboardTextYesNo[0])
                wb = true;
            else
                wb = false;
            temp = textSplitted[3].Substring(queryTextsParts[2].Length);
            if (temp == keyboardTextYesNo[0])
                dns = true;
            else
                dns = false;
            temp = textSplitted[4].Substring(queryTextsParts[3].Length);
            for (int i = 0; i < keyboardTextSort.Length; i++)
            {
                if (keyboardTextSort[i] == temp)
                {
                    sort = (Sort)i;
                    break;
                }
            }
            temp = textSplitted[5].Substring(queryTextsParts[4].Length);
            page = int.Parse(temp);
            if (query.Data == keyboardTextNextBack[0])
                page++;
            if (query.Data == keyboardTextNextBack[1])
                page--;
            if (page < 1)
                page = 1;

            if (string.IsNullOrWhiteSpace(text))
                text = "";
            string URL = APIURL + "/?text=" + text.Replace(" ", "+") + "&ozon=" + ozon + "&wb=" + wb + "&dns=" + dns + "&sort=" + sort + "&page=" + (page - 1) + "&count=" + maxCountOfProductsPerRequest;
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
                logger.LogError("Таймаут API в боте");
                await Message(query.Message!.Chat.Id, "Поиск не удался, повторите попытку позже");
                return;
            }
            GlobalVariables.SearchRequestsCount--;

            if (results == null || results.Products == null)
            {
                await Message(query.Message!.Chat.Id, "Поиск не удался, повторите попытку позже");
                return;
            }
            // Отправка результатов
            foreach (var product in results.Products)
            {
                await Message(query.Message!.Chat.Id, product.ProductURI + "\nЦена: " + product.Price + "\nРейтинг: " + product.Rating + "\nОтзывов: " + product.ReviewsCount);
                await Task.Delay(25);
            }

            var s = query.Message.Text.Split('\n');
            string t = string.Empty;
            for (int i = 0; i < s.Length - 1; i++)
            {
                t += s[i] + "\n";
            }
            t += queryTextsParts[STAGES_COUNT] + page;
            await SendMenu(query.Message!.Chat.Id, t, keyboardsSearching[0]);
        }
    }
}
