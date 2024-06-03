using ConsoleTelegramBot.Models;
using System.IO;
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
        await HandlePhotoAsync(botClient, update, cancellationToken);

    }
    private static async Task HandlePhotoAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update == null || update.Message == null || update.Message.Photo == null)
        {
            return;
        }
        long chatid = update.Message!.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
        ConsoleTelegramBot.Models.User user = context.Users.FirstOrDefault(x => x.TelegramId == chatid)!;

        string caption = update.Message!.Caption!;
        string id = caption.Replace("/UImage", "").Replace(" ", "");
        if (user != null && user.RoleId == 2 && caption.Contains("/UImage") && int.TryParse(id, out _))
        {
            Product product = context.Products.FirstOrDefault(x => x.Id == int.Parse(id))!;
            if (product != null)
            {
                await DowloadPhoto(botClient, update, int.Parse(id), cancellationToken);
            }
        }
    }

    private static async Task DowloadPhoto(ITelegramBotClient botClient, Update update, int IdProduct, CancellationToken cancellationToken)
    {
        var fileId = update.Message!.Photo!.Last().FileId;
        var fileInfo = await botClient.GetFileAsync(fileId);
        var filePath = fileInfo.FilePath;

        string url = @$"https://api.telegram.org/file/bot{ConsoleTelegramBot.Properties.Resources.Token}/{filePath}";
        string newnamefile = $@"{Thread.CurrentThread.ManagedThreadId}{Path.GetFileName(filePath!)}";
        string localpath = @$"Images\{newnamefile}";

        using (var client = new HttpClient())
        {
            using (var s = client.GetStreamAsync(url))
            {
                using (var fs = new FileStream(localpath, FileMode.OpenOrCreate))
                {
                    s.Result.CopyTo(fs);
                }
            }
        }

        ClothingStoreContext context = new ClothingStoreContext();
        Product product = context.Products.First(x => x.Id == IdProduct);
        product.Image = System.IO.File.ReadAllBytes(localpath);
        await context.SaveChangesAsync();

        System.IO.File.Delete(localpath);
        await botClient.SendTextMessageAsync(
                  chatId: update.Message.Chat.Id,
                  text: "Фото установлено!",
                  cancellationToken: cancellationToken);

    }
    private static async Task HandleCallBackDataAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update == null || update.CallbackQuery == null)
            return;
        long chatid = update.CallbackQuery.Message!.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
        ConsoleTelegramBot.Models.User user = context.Users.FirstOrDefault(x => x.TelegramId == chatid)!;
        if (user == null || user.RoleId == 1)
        {
            await HandleCallBackDataUserAsync(botClient, update, cancellationToken);
        }
        else if (user.RoleId == 2)
        {
            await HandleCallBackDataAdminAsync(botClient, update, cancellationToken);
        }
    }
    private static async Task HandleCallBackDataAdminAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        switch (update.CallbackQuery.Data)
        {
            case "start1":
                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "text",
                  cancellationToken: cancellationToken);
                break;
        }
    }
    private static async Task HandleCallBackDataUserAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        ClothingStoreContext context = new ClothingStoreContext();
        List<Brand> brands = context.Brands.ToList();
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        switch (update.CallbackQuery.Data)
        {
            case "ProductPants": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 8))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductShoes":
                foreach (var prod in context.Products.Where(x => x.CategoryId == 1))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}"; //tak zhe
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ", //tak zhe
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductOuterwear": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 2))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $"  \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductTshirt": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 3))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductShirt": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 4))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductLongSleeve": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 5))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductAccessories": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 6))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductHat": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 7))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductShorts": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 9))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductSocks": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 10))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductHoodie": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 11))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
            case "ProductUnderwear": //добавить такие же кнопки

                foreach (var prod in context.Products.Where(x => x.CategoryId == 12))
                {
                    string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
                    string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цвет: {prod.Color}";
                    if (prod.Image == null)
                    {
                        await botClient.SendPhotoAsync(
                        chatId: chatId,
                           photo: InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg"),
                           caption: @$"<b> {prod.Title} </b> {text} ",
                           parseMode: ParseMode.Html,
                           cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Stream stream = new MemoryStream(prod.Image!);
                        await botClient.SendPhotoAsync(
                         chatId: chatId,
                         photo: InputFile.FromStream(stream),
                         caption: @$"<b> {prod.Title} </b> {text} ",
                         parseMode: ParseMode.Html,
                         cancellationToken: cancellationToken);
                    }
                }
                break;
        }
    }
    private static async Task HandleMessagesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        long chatid = update.Message!.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
        ConsoleTelegramBot.Models.User user = context.Users.FirstOrDefault(x => x.TelegramId == chatid)!;
        if (user == null || user.RoleId == 1)
        {
            await HandleMessagesUserAsync(botClient, update, cancellationToken);
        }
        else if (user.RoleId == 2)
        {
            await HandleMessagesAdminsAsync(botClient, update, cancellationToken);
        }

    }
    private static async Task HandleMessagesAdminsAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        ClothingStoreContext context = new ClothingStoreContext();
        long chatId = update.Message!.Chat.Id;
        Message message = update.Message!;
        string text;
        switch (message.Text)
        {
            case "/start":

                await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "/start - для вывода команд\n" +
                  "/all_product1 - для вывода товаров\n" +
                  "Полей - ID, Title, Price, Size, Color\n" +
                  "/all_product2 - для вывода товаров\n" +
                  "Полей - ID, Title, Brand, Categories, Image\n" +
                  "Для изменения (добавления) картинки у продукта отправьте картинку и в комментарии (подписи) к ней напишите\n" + 
                  "/UImage _IDПродукта - например /UImage 16",
                  replyMarkup: new ReplyKeyboardRemove(),
                  cancellationToken: cancellationToken);
                break;
            case "/all_product1":

                text = $"Вывод всех товаров\n" +
                          $"ID |\tTitle |\tPrice |\t\tSize |\t\tColor\n";
                foreach (var item in context.Products)
                {
                    text += $"{item.Id} |\t{item.Title} |\t{item.Price} |\t{item.Size} |\t{item.Color}\n";
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text,
                    cancellationToken: cancellationToken);
                break;
            case "/all_product2":
                List<Brand> brands = context.Brands.ToList();
                List<Category> categories = context.Categories.ToList();
                text = $"Вывод всех товаров\n" +
                          $"ID |\tTitle |\tBrand |\tCategories |\tImage\n";
                foreach (var item in context.Products)
                {
                    string image = item.Image != null ? "да" : "нет";
                    string brand = brands.First(x => x.Id == item.BrandId).Brand1!;
                    string category = categories.First(x => x.Id == item.CategoryId).Title!; ;
                    text += $"{item.Id} |\t{item.Title} |\t{brand} |\t{category} |\t{image} \n";
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text,
                    cancellationToken: cancellationToken);
                break;
        }
    }
    private static async Task AddNewUserDBAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
        ConsoleTelegramBot.Models.User user = new ConsoleTelegramBot.Models.User();
        user.TelegramId = chatId;
        user.FullName = message.Chat.FirstName + message.Chat.LastName;
        user.RoleId = 1;
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
    private static async Task HandleMessagesUserAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
        ConsoleTelegramBot.Models.User user = context.Users.FirstOrDefault(x => x.TelegramId == chatId)!;
        if (user == null)
        {
            await AddNewUserDBAsync(botClient, update, cancellationToken);
        }


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
                            text: "Лонгсливы",
                            callbackData: "ProductLongSleeve"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "Аксессуары",
                            callbackData: "ProductAccessories"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Верхняя одежда",
                            callbackData: "ProductOuterwear"),
                         InlineKeyboardButton.WithCallbackData( 
                            text: "Футболки",
                            callbackData: "ProductTshirt"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Обувь",
                            callbackData: "ProductShoes"),

                    },
                    new []
                    {
                       InlineKeyboardButton.WithCallbackData(
                            text: "Головные уборы",
                            callbackData: "ProductHat"),
                       InlineKeyboardButton.WithCallbackData(
                            text: "Брюки",
                            callbackData: "ProductPants"),
                       InlineKeyboardButton.WithCallbackData(
                            text: "Шорты",
                            callbackData: "ProductShorts"),
                    },
                    new []
                    {
                        
                        InlineKeyboardButton.WithCallbackData(
                            text: "Носки",
                            callbackData: "ProductSocks"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "Худи",
                            callbackData: "ProductHoodie"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "Белье",
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