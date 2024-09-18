using System;
using GroqSharp;
using GroqSharp.Models;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static TelegramBotClient botClient = null!;
    private static readonly Dictionary<long, bool> GroqRequestState = new();

    private readonly static string GROQ_API_KEY;
    private readonly static string TELEGRAM_API_KEY;
    private readonly static string  GROQ_API_MODEL;

    static Program()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        GROQ_API_KEY = configuration["ApiKeys:GroqApiKey"] ?? throw new ArgumentException("GroqApiKey is not set");
        TELEGRAM_API_KEY = configuration["ApiKeys:TelegramApiKey"] ?? throw new ArgumentException("TelegramApiKey is not set");
        GROQ_API_MODEL = configuration["ApiConfigurations:GroqConfiguration:GroqApiModel"] ?? throw new ArgumentException("GroqApiModel is not set");
    }

    static async Task Main()
    {
        using var cts = new CancellationTokenSource();
        botClient = new TelegramBotClient(TELEGRAM_API_KEY, cancellationToken: cts.Token);

        var me = await botClient.GetMeAsync();

        botClient.OnMessage += OnMessageAsync;
        botClient.OnUpdate += OnUpdateAsync;

        Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
        Console.ReadLine();
        
        cts.Cancel();
    }

    private static async Task OnMessageAsync(Telegram.Bot.Types.Message msg, UpdateType type)
    {
        if (msg.Type == MessageType.Text)
        {
            string messageText = msg.Text!;

            Console.WriteLine(msg.From + " " + messageText);

            if (messageText == "/start")
            {
                await ShowMainMenuAsync(msg.Chat.Id);
            }
            else if(GroqRequestState[msg.Chat.Id])
            {
                var response = await ReturnGroqResponseAsync(messageText);

                await botClient.SendTextMessageAsync(msg.Chat.Id, "Response: " + response);

                await ShowMainMenuAsync(msg.Chat.Id);

                GroqRequestState[msg.Chat.Id] = false;
            }
            else
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id, "You chose: " + messageText);
            }
        }
    }


    private static async Task OnUpdateAsync(Update update)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            var callbackQuery = update.CallbackQuery!;
            var callbackData = callbackQuery.Data;

            var chatId = callbackQuery.Message!.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;

            switch(callbackData)
            {
                case "student":
                    await SendMessageResponseWithBackButtonAsync(chatId, messageId, 
                        "<b>ст. Маланічев Д.А.</b> гр. ІС-13");
                    break;
                case "it_technologies":
                    await SendMessageResponseWithBackButtonAsync(chatId, messageId, 
                        "<b>ІТ-технології:</b> Front-end Back-End WEB-технології");
                    break;
                case "contacts":
                    await SendMessageResponseWithBackButtonAsync(chatId, messageId, 
                        "<b>Контакти:</b> телефон 123-45-45-45 e-mail: malanichev.denys@lll.kpi.ua");
                    break;
                case "chatgpt":
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Введіть ваш запит:",
                        parseMode: ParseMode.Html
                    );

                    GroqRequestState[chatId] = true;

                    break;
                case "back_to_menu":
                    await ShowMainMenuAsync(chatId, messageId);
                    break;
            }
                
        }
    }

    private static async Task ShowMainMenuAsync(long chatId, int? messageId = null)
    {
        var buttons = new InlineKeyboardButton[][]
               {
                    [
                        InlineKeyboardButton.WithCallbackData("Student", "student"),
                        InlineKeyboardButton.WithCallbackData("IT-technologies", "it_technologies")
                    ],
                    [
                        InlineKeyboardButton.WithCallbackData("Contacts", "contacts"),
                        InlineKeyboardButton.WithCallbackData("ChatGPT", "chatgpt")
                    ],
               };

        var inline = new InlineKeyboardMarkup(buttons);

        if (messageId is not null)
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: (int)messageId,
                text: "<b>Bас вітає чат-бот!</b> Виберіть відповідну команду:",
                replyMarkup: inline,
                parseMode: ParseMode.Html
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "<b>Bас вітає чат-бот!</b> Виберіть відповідну команду:",
                replyMarkup: inline,
                parseMode: ParseMode.Html
            );
        }
    }

    private static async Task SendMessageResponseWithBackButtonAsync(long chatId, int messageId, string message)
    {
        var backButton = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Back", "back_to_menu")
        });

        await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: message,
            replyMarkup: backButton,
            parseMode: ParseMode.Html
        );
    }


    private static async Task<string> ReturnGroqResponseAsync(string messageText)
    {
        var groqClient = new GroqClient(GROQ_API_KEY, GROQ_API_MODEL)
            .SetTemperature(0.5)
            .SetMaxTokens(256)
            .SetStop("NONE");

        return await groqClient.CreateChatCompletionAsync(
            new GroqSharp.Models.Message { Role = MessageRoleType.User, Content = messageText });
    }

    private static void SetConfiguration()
    {
        
    }
}
