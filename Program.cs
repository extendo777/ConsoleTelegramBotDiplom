using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal class Program
{
    private static void Main(string[] args)
    {
        Start();
        Console.ReadLine();
    }
    private static async void Start()
    {
        var botClient = new TelegramBotClient(ConsoleTelegramBot.Properties.Resources.Token);

        using CancellationTokenSource cts = new();
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();
    }
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await HandleMessagesAsync(botClient, update, cancellationToken);
        await HandleCallBackDataAsync(botClient, update, cancellationToken);

    }
    private static async Task HandleCallBackDataAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update == null || update.CallbackQuery == null)
            return;

        long chatId = update.CallbackQuery.Message!.Chat.Id;
        switch (update.CallbackQuery.Data)
        {
            case "start1":
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Привет",
                            callbackData: "level2_1"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "пока",
                            callbackData: "level2_2"),
                    },
                });

                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "Выберите",
                     replyMarkup: inlineKeyboard,
                     cancellationToken: cancellationToken);
                break;
            case "start2":
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Как дела?",
                    cancellationToken: cancellationToken);
                break;
            case "start3":
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Как дела?",
                    cancellationToken: cancellationToken);
                break;
           
            case "level3_1":
                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "level3_1",
                     cancellationToken: cancellationToken);
                break;
            case "level3_2":
                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "level3_2",
                     cancellationToken: cancellationToken);
                break;
            case "level3_3":
                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "level3_3",
                     cancellationToken: cancellationToken);
                break;
            default:
                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "Кнопка не обработана",
                     cancellationToken: cancellationToken);
                break;

        }
    }
    private static async Task HandleMessagesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;

        if (message.Text == "/start")
        {

            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { "📂Каталог", "🛍Корзина" },
                new KeyboardButton[] { "📦Заказы", "📣Новости" },
                new KeyboardButton[] { "⚙Настройки", "❓Помощь" },
            });

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите действие",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
            return;
        }
        switch (message.Text)
        {
            case "📂Каталог":
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Джинсы",
                            callbackData: "start1"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "Футболки",
                            callbackData: "start2"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Кроссовки",
                            callbackData: "start3"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Аксессуары",
                            callbackData: "start4"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "Сумки",
                            callbackData: "start5"),
                    },
                });

                await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: "Выберите раздел, чтобы вывести список товаров:",
                     replyMarkup: inlineKeyboard,
                     cancellationToken: cancellationToken);

                break;
            case "❓Помощь":
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Список команд:\n" +
                    "/catalog - Каталог\n" +
                    "/cart — Корзина\n" +
                    "/history — История заказов\n" +
                    "/news — Наши новости и акции\n" +
                    "/settings — Настройки\r\n" +
                    "/help — Справка\r\n" +
                    "/about — О проекте\r\n" +
                    "/start — Главное меню\r\n" +
                    "/off — Выключить подписку на бота\r\n" +
                    "/on — Включить подписку на бота\r\n" +
                    " \r\n" +
                    "Выберите ниже раздел справки и получите краткую помощь. Если Ваш вопрос не решен, обратитесь за помощью к живому оператору @f1nessef1nesse_33 \r\n", 
                    cancellationToken: cancellationToken);
                break;
                
        }
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

}