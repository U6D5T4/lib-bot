using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services;

public class MessageService : IMessageService
{
    const string EmojiNewInSquare = "\U0001F193";
    const string EmojiLock = "\U0001F512";
    private readonly ITelegramBotClient _botClient;
    public MessageService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task<Message> SendTextMessageAndClearKeyboardAsync(ITelegramBotClient bot, long chatId, string message)
    {
        return await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardRemove());
    }

    public async Task<Message> AskToEnterOutlookLoginAsync(ITelegramBotClient bot, Message message)
    {
        return await SendTextMessageAndClearKeyboardAsync(bot, message.Chat.Id,
            "Please, enter your outlook email or outlook login.");
    }

    public async Task<Message> AskToEnterAuthCodeAsync(ITelegramBotClient bot, Message message)
    {
        return await SendTextMessageAndClearKeyboardAsync(bot, message.Chat.Id,
            "Please, check your email and enter your auth code here.");
    }

    public async Task<Message> AksToEnterSearchQueryAsync(ITelegramBotClient bot, Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id,
            "Please, enter book's name or author's name.");
    }

    public async Task<Message> SayThisBookIsAlreadyBorrowAsync(ITelegramBotClient bot, Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id,
            "Sorry, this book is already borrowed.");
    }

    public async Task EditMessageAfterYesAndNoButtons(ITelegramBotClient bot, CallbackQuery callbackQuery, string message)
    {
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, message);
    }

    public async Task<Message> SendWelcomeMessageAsync(long chatId)
    {
        var message = "Hey, I'm LibBot. Choose the option";
        var replyMarkup = CreateReplyKeyboardMarkup("Library", "My Books");
        return await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup);
    }

   

    public async Task CreateYesAndNoButtons(CallbackQuery callbackQuery, string message)
    {
        var yesNoDict = new Dictionary<string, string>
        {
            {"No", "No" },
            {"Yes", "Yes" },
        };

        var inlineKeyboard = CreateInlineKeyboardMarkup(yesNoDict);
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, message, replyMarkup: inlineKeyboard);
    }

    public async Task<Message> DisplayBookButtons(long chatId, string messageText, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books, true);
        var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
        return await _botClient.SendTextMessageAsync(chatId, messageText, replyMarkup: inlineKeyboardMarkup);
    }

    public async Task UpdateBookButtons(Message message, List<BookDataResponse> books, bool firstPage)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books, firstPage);
        var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
        await _botClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, inlineKeyboardMarkup);
    }

    public async Task UpdateBookButtonsAndMessageText(long chatId, int messageId, string messageText, List<BookDataResponse> books, bool firstPage)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books, firstPage);
        var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
        await _botClient.EditMessageTextAsync(chatId, messageId, messageText, replyMarkup: inlineKeyboardMarkup);
    }

    public List<InlineKeyboardButton> CreateBookButtons(List<BookDataResponse> books, bool firstPage)
    {
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        foreach (BookDataResponse book in books)
        {
            var buttonText = string.Empty;
            if (book.TakenToRead.ToShortDateString() != new DateTime(01, 01, 0001).ToShortDateString())
            {
                var borrowedDate = book.TakenToRead.AddMonths(2).ToLocalTime().ToShortDateString();
                buttonText = book.Title + " Due Date:" + borrowedDate;
            }
            else
            {
                buttonText = (book.BookReaderId is null ? EmojiNewInSquare : EmojiLock) + $" {book.Title}";
            }

            var callbackData = book.BookReaderId is null ? book.Id.ToString() : "Borrowed";
            var button = InlineKeyboardButton.WithCallbackData(text: buttonText, callbackData: callbackData);
            buttons.Add(button);
        }          
        if(firstPage && buttons.Count <= SharePointService.AmountBooks)
        {
        }
        else if(firstPage)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Next", callbackData: "Next"));
        }
        else if (buttons.Count <= SharePointService.AmountBooks)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Previous", callbackData: "Previous"));
        }
        else
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Previous", callbackData: "Previous"));
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Next", callbackData: "Next"));
        }

        if(books.Count == SharePointService.AmountBooks + 1)
            buttons.RemoveAt(SharePointService.AmountBooks);
        return buttons;
    }

    public async Task DisplayInlineButtonsWithMessage(Message message, string messageText, params string[] buttons)
    {
        var buttonsDict = buttons.ToDictionary(key => key, val => val);
        var inlineButtons = CreateInlineKeyboardMarkup(buttonsDict);
        await _botClient.SendTextMessageAsync(message.Chat.Id, messageText, replyMarkup: inlineButtons);
    }

    public async Task UpdateInlineButtonsWithMessage(long chatId, int messageId, string messageText, string[] bookPaths)
    {
        var inlineKeyboardMarkup = GetInlineKeybordInTwoColumns(bookPaths.Select(key => key).Append("Clear filters").Append("Show all Books"));
        await _botClient.EditMessageTextAsync(chatId, messageId, messageText, replyMarkup: inlineKeyboardMarkup);
    }

    public async Task SendLibraryMenuMessageAsync(long chatId)
    {
        var replyMarkup = GetLibraryMenuMarkup();
        var message = $"Welcome to `Library` menu{Environment.NewLine}" +
                      $"`Search Books` - search all books in library by name{Environment.NewLine}" +
                      $"`Filter by path` - show books filtered by chosen paths{Environment.NewLine}" +
                      $"`Show all Books` - show all books in library";

        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup, parseMode: ParseMode.Markdown);
    }

    public async Task SendMessageWithInlineKeyboardAsync(long chatId, string message, string[] keys)
    {
        var inlineKeyboardMarkup = GetInlineKeybordInTwoColumns( keys.Select(key => key).Append("Clear filters").Append("Show all Books"));
        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: inlineKeyboardMarkup);
    }

    private InlineKeyboardMarkup GetInlineKeybordInTwoColumns(IEnumerable<string> keys)
    {
        var inlineButtons = keys.Select(key => InlineKeyboardButton.WithCallbackData(key)).ToArray();
        var inlineButtonsTwoColumns = new List<InlineKeyboardButton[]>();
        for (var i = 0; i < inlineButtons.Length; i++)
        {
            if (inlineButtons.Length - 1 == i)
            {
                inlineButtonsTwoColumns.Add(new[] { inlineButtons[i] });
            }
            else
                inlineButtonsTwoColumns.Add(new[] { inlineButtons[i], inlineButtons[i + 1] });
            i++;
        }

        return new InlineKeyboardMarkup(inlineButtonsTwoColumns.ToArray());
    }

    private List<InlineKeyboardButton> CreatePathButtons(string[] paths)
    {
        return paths.Select(path => InlineKeyboardButton.WithCallbackData(path)).ToList();
    }

    private ReplyKeyboardMarkup GetLibraryMenuMarkup()
    {
        return new ReplyKeyboardMarkup(new []
        {
            new KeyboardButton[] { "Search Books", "Filter by path" },
            new KeyboardButton[] { "Show all Books" },
            new KeyboardButton[] { "Cancel"}
        })
        {
            ResizeKeyboard = true
        };
    }

    private ReplyKeyboardMarkup CreateReplyKeyboardMarkup(params string[] nameButtons)
    {
        List<KeyboardButton> buttons = new List<KeyboardButton>();

        foreach (string button in nameButtons)
        {
            buttons.Add(new KeyboardButton(button));
        }

        var replyButtons = new ReplyKeyboardMarkup(buttons);
        replyButtons.ResizeKeyboard = true;

        return replyButtons;
    }
    private InlineKeyboardMarkup CreateInlineKeyboardMarkup(Dictionary<string, string> inlineButtons)
    {
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        foreach (var inlineButton in inlineButtons)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: inlineButton.Key, callbackData: inlineButton.Value));
        }

        return new InlineKeyboardMarkup(buttons);
    }
    private InlineKeyboardMarkup SetInlineKeyboardInColumn(List<InlineKeyboardButton> inlineButtons)
    {
        var inlineButtonsColumn = new List<InlineKeyboardButton[]>();
        for (var i = 0; i < inlineButtons.Count; i++)
        {
            inlineButtonsColumn.Add(new[] { inlineButtons[i] });
        }

        return new InlineKeyboardMarkup(inlineButtonsColumn.ToArray());
    }
}