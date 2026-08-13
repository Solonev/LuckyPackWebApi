using LuckyPackWebApi.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LuckyPackWebApi.Background;

public class TelegramBotWorker(
    ITelegramBotClient botClient,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramBotWorker> logger
)
    : BackgroundService
{
    private readonly string _webAppUrl = telegramOptions.Value.WebAppUrl;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            //Обновляем кнопку на всякий случай
            await botClient.SetChatMenuButton(
                menuButton: new MenuButtonWebApp
                {
                    Text = "Каталог",
                    WebApp = new WebAppInfo
                    {
                        Url = _webAppUrl
                    }
                },
                cancellationToken: ct);
            
            logger.LogInformation("Telegram bot starting...");
            
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: new ReceiverOptions
                {
                    AllowedUpdates = [],
                    DropPendingUpdates = false
                },
                cancellationToken: ct
            );
            
            logger.LogInformation("Telegram bot started");
            
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            logger.LogInformation("Telegram bot stopping...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Telegram bot terminated unexpectedly");
            throw;
        }
    }
    
    private readonly Manager _manager = new();
    
    private class Manager
    {
        public readonly Dictionary<long, string> ChatStates = new();
        
        public bool CheckStatus(Update update, ChatState state)
        {
            return
                update.Message != null &&
                CheckStatus(update.Message.Chat.Id, state);
        }
        
        public bool CheckStatus(long chatId, ChatState state)
        {
            return ChatStates.TryGetValue(chatId, out var chatState) &&
                Enum.TryParse<ChatState>(chatState, out var result) &&
                state == result;
        }
        
        public void Set(long chatId, ChatState state)
        {
            ChatStates[chatId] = state.ToString();
        }
    }
    
    
    private readonly Dictionary<long, ContactInfo> _contacts = new();
    
    private class ContactInfo
    {
        public string UserId { get; set; }
        public string ChatId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }
    
    /// <summary>
    /// Обрабатывает входящие сообщения
    /// </summary>
    private async Task HandleUpdateAsync(
        ITelegramBotClient client,
        Update update,
        CancellationToken ct
    )
    {
        //Логирование
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Received Telegram update: {UpdateType}",
                update.Type
            );
        
        //Обработать телефон
        if (
            _manager.CheckStatus(update, ChatState.WaitPhone) &&
            update.Message?.Contact is { } contact
        )
        {
            var phone = contact.PhoneNumber;
            var telegramUserId = update.Message!.From!.Id;
            var contactUserId = contact.UserId;
            
            if (telegramUserId != contactUserId)
            {
                //TODO: настроить флоу если отправляют не свой контакт
            }
            
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Phone: {phone}", phone);
                logger.LogInformation("Telegram ID: {telegramUserId}", telegramUserId);
            }
        }
        
        //Обработать Callback
        if (update.CallbackQuery != null)
        {
            await HandleInlineCallbackAsync(update.CallbackQuery, ct);
            return;
        }
        
        //Обработка всех остальных сообщений - отправка клавиатуры с каталогом
        if (update.Message != null)
        {
            await ShowMainMenuAsync(client, update.Message.Chat.Id, ct);
        }
    }
    
    private Task<Message> ShowMainMenuAsync(
        ITelegramBotClient client,
        long chatId,
        CancellationToken ct
    )
    {
        _manager.Set(chatId, ChatState.Main);
        
        return client.SendMessage(
            chatId: chatId,
            text: "Главное меню:",
            replyMarkup: new InlineKeyboardMarkup(
            [
                [InlineKeyboardButton.WithWebApp("🛍 Каталог", new WebAppInfo(_webAppUrl))],
                [InlineKeyboardButton.WithCallbackData("📚 База знаний", nameof(CallBacksCommands.KnowledgeBase))],
                [InlineKeyboardButton.WithCallbackData("👤 Личный кабинет", nameof(CallBacksCommands.Lk))],
            ]),
            cancellationToken: ct
        );
    }
    
    /// <summary>
    /// Обрабатывает Inline-callback'и
    /// </summary>
    private async Task HandleInlineCallbackAsync(
        CallbackQuery botCallbackQuery,
        CancellationToken ct)
    {
        var chatId = botCallbackQuery.From.Id;
        
        switch (botCallbackQuery.Data)
        {
            case nameof(CallBacksCommands.Catalog):
                
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Личный кабинет:",
                    replyMarkup: new InlineKeyboardMarkup(
                    [
                        [InlineKeyboardButton.WithCallbackData("Пакеты, Окна, Fresh", nameof(CallBacksCommands.Undefined))],
                        [InlineKeyboardButton.WithCallbackData("Коробки", nameof(CallBacksCommands.Undefined))],
                        [InlineKeyboardButton.WithCallbackData("Вазы", nameof(CallBacksCommands.Undefined))],
                        [InlineKeyboardButton.WithCallbackData("Лента", nameof(CallBacksCommands.Undefined))],
                        [InlineKeyboardButton.WithCallbackData("Лента", nameof(CallBacksCommands.MainMenu))],
                    ]),
                    cancellationToken: ct);
                
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: botCallbackQuery.Id,
                    cancellationToken: ct);
                
                break;
            
            case nameof(CallBacksCommands.KnowledgeBase):
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Функционал ещё в разработке.",
                    cancellationToken: ct
                );
                await botClient.AnswerCallbackQuery(
                    botCallbackQuery.Id,
                    cancellationToken: ct
                );
                await ShowMainMenuAsync(botClient, chatId, ct);
                break;
            
            case nameof(CallBacksCommands.Lk):
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Пожалуйста, заполните личную информацию:",
                    replyMarkup: new InlineKeyboardMarkup(
                    [
                        [InlineKeyboardButton.WithCallbackData("Ввести телефон", nameof(CallBacksCommands.SetUserPhone))],
                        [InlineKeyboardButton.WithCallbackData("Ввести email", nameof(CallBacksCommands.SetUserEmail))],
                    ]),
                    cancellationToken: ct
                );
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: botCallbackQuery.Id,
                    cancellationToken: ct);
                break;
            
            case nameof(CallBacksCommands.SetUserPhone):
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Пожалуйста, введите телефон:",
                    replyMarkup: new ReplyKeyboardMarkup(
                        [
                            [new KeyboardButton("📱 Отправить номер телефона") { RequestContact = true }]
                        ]
                    )
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    },
                    cancellationToken: ct
                );
                _manager.Set(chatId, ChatState.Main);
                
                break;
        }
    }
    
    /// <summary>
    /// Обработка ошибок
    /// </summary>
    private Task HandleErrorAsync(
        ITelegramBotClient client,
        Exception exception,
        HandleErrorSource errorSource,
        CancellationToken ct
    )
    {
        logger.LogError(
            exception,
            "Telegram bot error. Source: {ErrorSource}",
            errorSource
        );
        
        return Task.CompletedTask;
    }
}