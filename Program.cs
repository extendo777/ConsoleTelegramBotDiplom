using System.Collections.Generic;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ConsoleTelegramBot.Models;
using System.Reflection.Metadata.Ecma335;

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

    }
    private static async Task HandleCallBackDataUserAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        ClothingStoreContext context = new ClothingStoreContext();
        List<Brand> brands = context.Brands.ToList();
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        switch (update.CallbackQuery.Data)
        {
            case "ProductPants":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 8).ToList(), cancellationToken);
                break;
            case "ProductShoes":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 1).ToList(), cancellationToken);
                break;
            case "ProductOuterwear":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 2).ToList(), cancellationToken);
                break;
            case "ProductTshirt":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 3).ToList(), cancellationToken);
                break;
            case "ProductShirt":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 4).ToList(), cancellationToken);
                break;
            case "ProductLongSleeve":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 5).ToList(), cancellationToken);
                break;
            case "ProductAccessories":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 6).ToList(), cancellationToken);
                break;
            case "ProductHat":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 7).ToList(), cancellationToken);
                break;
            case "ProductShorts":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 9).ToList(), cancellationToken);
                break;
            case "ProductSocks":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 10).ToList(), cancellationToken);
                break;
            case "ProductHoodie":
                await SelectProductUserAsync(botClient, update, context.Products.Where(x => x.CategoryId == 11).ToList(), cancellationToken);
                break;
        }
        if (update.CallbackQuery.Data!.Contains("ToBuy"))
        {
            int id = int.Parse(update.CallbackQuery.Data.Replace("ToBuy", ""));
            Cart cart = new Cart();
            cart.ProductId = id;
            cart.User = context.Users.FirstOrDefault(x => x.TelegramId == chatId)!;
            context.Carts.Add(cart);
            await context.SaveChangesAsync();

            await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "Для подтверждения заказа свяжитесь с @f1nessef1nesse_33",
                  cancellationToken: cancellationToken);
            ConsoleTelegramBot.Models.User user = context.Users.FirstOrDefault(x => x.TelegramId == chatId)!;
            foreach (var item in context.Users.Where(x => x.RoleId == 2))
            {

                await botClient.SendTextMessageAsync(
                   chatId: item.TelegramId!,
                   text: $"Пользователь {user.Id} {user.FullName} @{user.UserName} Хочет купить товар",
                   cancellationToken: cancellationToken);
            }

        }
        else if (update.CallbackQuery.Data!.Contains("ToCart"))
        {
            int id = int.Parse(update.CallbackQuery.Data.Replace("ToCart", ""));
            Cart cart = new Cart();
            cart.ProductId = id;
            cart.User = context.Users.FirstOrDefault(x => x.TelegramId == chatId)!;
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
            await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "Ваш заказ добавлен в корзину",
                  cancellationToken: cancellationToken);
        }
        else if (update.CallbackQuery.Data!.Contains("DeleteCart"))
        {
            int id = int.Parse(update.CallbackQuery.Data.Replace("DeleteCart", ""));
            Cart cart = context.Carts.FirstOrDefault(x => x.Id == id)!;
            context.Carts.Remove(cart);
            await context.SaveChangesAsync();
            await botClient.SendTextMessageAsync(
                  chatId: chatId,
                  text: "Ваш заказ удалён из корзину",
                  cancellationToken: cancellationToken);
        }
    }

    private static async Task SelectProductUserAsync(ITelegramBotClient botClient, Update update, List<Product> products, CancellationToken cancellationToken)
    {
        ClothingStoreContext context = new ClothingStoreContext();
        List<Brand> brands = context.Brands.ToList();
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        foreach (var prod in products)
        {
            string brand = brands.First(x => x.Id == prod.BrandId).Brand1!;
            string text = $" \n Бренд: {brand}\n Размер: {prod.Size}\n Цена: {prod.Price} ";
            InputFile input;
            if (prod.Image == null)
            {
                input = InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg");
            }
            else
            {
                Stream stream = new MemoryStream(prod.Image!);
                input = InputFile.FromStream(stream);
            }

            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "В корзину",
                            callbackData: $"ToCart{prod.Id}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "Купить",
                            callbackData: $"ToBuy{prod.Id}")
                    },
            });

            await botClient.SendPhotoAsync(
               chatId: chatId,
               photo: input,
               caption: @$"<b> {prod.Title} </b> {text} ",
               parseMode: ParseMode.Html,
               replyMarkup: inlineKeyboard,
               cancellationToken: cancellationToken);

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
                  "/Cart _IDПользователя - для вывода корзины выбранного пользвателя\n" +
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
        if (message.Text!.Contains("/Cart "))
        {
            if (int.TryParse(message.Text.Replace("/Cart ", ""), out int userid))
            {

                List<ConsoleTelegramBot.Models.User> users = context.Users.ToList();
                List<Product> products = context.Products.ToList();
                text = $"Вывод корзины пользователя\n" +
                          $"ID |\tUserName |\tIDProd|\tNameProd \n";
                foreach (var item in context.Carts.Where(x => x.UserId == userid))
                {
                    string username = users.FirstOrDefault(x => x.Id == userid)!.UserName!;
                    string product = products.FirstOrDefault(x => x.Id == item.ProductId)!.Title!;
                    text += $"{item.Id} |\t{username} |\t{item.ProductId} |\t{product}\n";
                }

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text,
                    cancellationToken: cancellationToken);

                return;
            }
            await botClient.SendTextMessageAsync(
                   chatId: chatId,
                   text: "Пользователь не найден",
                   cancellationToken: cancellationToken);
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
        user.UserName = message.Chat.Username;
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
                new KeyboardButton[] { "📂Каталог", "🛍Корзина", "❓Помощь" },
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите действие",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
            return;
        }
        switch (message.Text)
        {
            case "/catalog":
                await SelectCatalogAsync(botClient, update, cancellationToken);
                break;
            case "📂Каталог":
                await SelectCatalogAsync(botClient, update, cancellationToken);
                break;
            case "/cart":
                await SelecCartAsync(botClient, update, cancellationToken);
                break;
            case "🛍Корзина":
                await SelecCartAsync(botClient, update, cancellationToken);
                break;
            case "❓Помощь":
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Список команд:\n" +
                    "/catalog - Каталог\n" +
                    "/cart — Корзина\n" +
                    "/start — Главное меню\r\n" +
                    " \r\n" +
                    "Выберите ниже раздел справки и получите краткую помощь. Если Ваш вопрос не решен, обратитесь за помощью к живому оператору @f1nessef1nesse_33 \r\n",
                    cancellationToken: cancellationToken);
                break;


        }
    }

    private static async Task SelectCatalogAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
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
    }
    private static async Task SelecCartAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        ClothingStoreContext context = new ClothingStoreContext();
        ConsoleTelegramBot.Models.User selectuser = context.Users.FirstOrDefault(x => x.TelegramId == chatId)!;
        List<Product> products = context.Products.ToList();
        List<Brand> brands = context.Brands.ToList();
        List<Category> categories = context.Categories.ToList();

        string text = context.Carts.Where(x => x.Id == selectuser.Id).Count() == 0 ?
            "Ваша корзина пуста\n" : $"Корзина:\n";
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            cancellationToken: cancellationToken);
        int i = 0;
        foreach (var item in context.Carts.Where(x => x.UserId == selectuser.Id))
        {
            i++;
            Product product = products.FirstOrDefault(x => x.Id == item.ProductId)!;
            Brand brand = brands.FirstOrDefault(x => x.Id == product.BrandId)!;
            Category category = categories.FirstOrDefault(x => x.Id == product.CategoryId)!;

            string selecttext = $" \n Бренд: {brand.Brand1}\n Размер: {product.Size}\n Цена: {product.Price} ";
            InputFile input;
            if (product.Image == null)
            {
                input = InputFile.FromUri("https://raw.githubusercontent.com/extendo777/Images/main/NoImage.jpg");
            }
            else
            {
                Stream stream = new MemoryStream(product.Image!);
                input = InputFile.FromStream(stream);
            }

            InlineKeyboardMarkup inlineKeyboardButtons = new(new[]
            {
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData(
                                text: "Удалить",
                                callbackData: $"DeleteCart{item.Id}"),
                        },
                    });

            await botClient.SendPhotoAsync(
               chatId: chatId,
               photo: input,
               caption: @$"<b>{i}. {product.Title} </b> {selecttext} ",
               parseMode: ParseMode.Html,
               replyMarkup: inlineKeyboardButtons,
               cancellationToken: cancellationToken);
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