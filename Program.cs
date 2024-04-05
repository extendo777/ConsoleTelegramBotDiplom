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
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;

        if (message.Text == "/start")
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithUrl(
                        text: "Dota 2",
                        url: "https://www.dota2.com/home"),
                    InlineKeyboardButton.WithUrl(
                        text: "Minecraft",
                        url: "https://www.minecraft.net/ru-ru"),
                },
                new []
                {
                    InlineKeyboardButton.WithUrl(
                        text: "Yandex",
                        url: "https://ya.ru/"),
                    InlineKeyboardButton.WithUrl(
                        text: "StackOverFlow",
                        url: "https://stackoverflow.com/"),
                },
                 new []
                {
                    InlineKeyboardButton.WithUrl(
                        text: "Мануал о создании",
                        url: "https://telegrambots.github.io/book/index.html"),
                    InlineKeyboardButton.WithUrl(
                        text: "О нас",
                        url: "https://1-mok.mskobr.ru/postuplenie-v-kolledzh/priemnaya-komissiya"),
                },
            });

           await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите ссылку",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);


            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { "Dota 2", "Minecraft" },
                new KeyboardButton[] { "Yandex", "StackOverFlow" },
                new KeyboardButton[] { "Мануал о создании", "О нас" },
            });

           await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Добавление кнопок",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
            return;
        }
        switch (message.Text)
        {
            case "Dota 2":
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "https://www.dota2.com/home",
                cancellationToken: cancellationToken);
                break;
            case "Minecraft":
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "https://www.minecraft.net/ru-ru",
                  cancellationToken: cancellationToken);
                break;
            case "Yandex":
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "https://ya.ru/",
                  cancellationToken: cancellationToken);
                break;
            case "StackOverFlow":
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "https://stackoverflow.com/",
                  cancellationToken: cancellationToken);
                break;
            case "Мануал о создании":
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "https://telegrambots.github.io/book/index.html",
                  cancellationToken: cancellationToken);
                break;
            case "О нас":
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "https://1-mok.mskobr.ru/postuplenie-v-kolledzh/priemnaya-komissiya",
                  cancellationToken: cancellationToken);
                break;
            default:
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "Сообщене не распознано",
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