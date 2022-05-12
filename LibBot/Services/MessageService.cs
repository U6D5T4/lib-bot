using LibBot.Models;
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

    public async Task<Message> SendTextMessageAndClearKeyboardAsync(long chatId, string message)
    {
        return await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardRemove());
    }

    public async Task<Message> AskToEnterOutlookLoginAsync(Message message)
    {
        return await SendTextMessageAndClearKeyboardAsync(message.Chat.Id,
            "Please, enter your outlook email or outlook login.");
    }

    public async Task<Message> AskToEnterAuthCodeAsync(Message message)
    {
        return await SendTextMessageAndClearKeyboardAsync(message.Chat.Id,
            "Please, check your email and enter your auth code here.");
    }

    public async Task<Message> AksToEnterSearchQueryAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id,
            "Please, enter book's name or author's name.");
    }

    public async Task<Message> SayThisBookIsAlreadyBorrowAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id,
            "Sorry, this book is already borrowed.");
    }

    public async Task EditMessageAfterYesAndNoButtonsAsync(CallbackQuery callbackQuery, string message)
    {
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, message);
    }

    public async Task<Message> SendWelcomeMessageAsync(long chatId)
    {
        var message = "Hey, I'm LibBot. Choose the option";
        var replyMarkup = GetMainMenu();
        return await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup);
    }

    public async Task CreateYesAndNoButtonsAsync(CallbackQuery callbackQuery, string message)
    {
        var yesNoDict = new Dictionary<string, string>
        {
            {"No", "No" },
            {"Yes", "Yes" },
        };

        var inlineKeyboard = CreateInlineKeyboardMarkup(yesNoDict);
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, message, replyMarkup: inlineKeyboard);
    }

    public async Task<Message> DisplayBookButtons(long chatId, string messageText, List<BookDataResponse> books, ChatState chatState)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtonsAsync(books, true);
        var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
        return await _botClient.SendTextMessageAsync(chatId, messageText, replyMarkup: inlineKeyboardMarkup);
    }

    public async Task CreateUserBookButtonsAsync(long chatId, List<BookDataResponse> books)
    {
        var sortedBooks = books.OrderBy(x => x.TakenToRead).ToList();
        var buttons = new List<InlineKeyboardButton>();
        var returnDateList = new List<string>();
        var isNeedSendMessage = false;
        var messageText = string.Empty;

        foreach (BookDataResponse book in sortedBooks)
        {
            var returnDate = book.TakenToRead.Value.AddMonths(2).ToLocalTime().ToShortDateString();
            if (!returnDateList.Contains(returnDate))
            {
                returnDateList.Add(returnDate);

                if (isNeedSendMessage)
                {
                    var inlineButtons = SetInlineKeyboardInColumn(buttons);
                    await _botClient.SendTextMessageAsync(chatId, messageText, replyMarkup: inlineButtons);
                    buttons = new List<InlineKeyboardButton>();
                }
                isNeedSendMessage = true;
                messageText = "Return till " + returnDate;
            }
            var buttonText = book.Title;
            var callbackData = book.Id.ToString();
            var button = InlineKeyboardButton.WithCallbackData(text: buttonText, callbackData: callbackData);
            buttons.Add(button);
        }

        if (isNeedSendMessage)
        {
            var inlineButtons = SetInlineKeyboardInColumn(buttons);
            await _botClient.SendTextMessageAsync(chatId, messageText, replyMarkup: inlineButtons);
        }
    }

    private List<InlineKeyboardButton> CreateUserButtonsForUpdate(List<BookDataResponse> books)
    {
        var sortedBooks = books.OrderBy(x => x.TakenToRead).ToList();
        var buttons = new List<InlineKeyboardButton>();
        foreach (var book in sortedBooks)
        {
            var buttonText = book.Title;
            var callbackData = book.Id.ToString();
            var button = InlineKeyboardButton.WithCallbackData(text: buttonText, callbackData: callbackData);
            buttons.Add(button);
        }

        return buttons;
    }

    public async Task UpdateBookButtons(Message message, List<BookDataResponse> books, bool firstPage, ChatState chatState)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtonsAsync(books, firstPage);
        var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
        await _botClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, inlineKeyboardMarkup);
    }

    public async Task UpdateUserBookButtonsAsync(Message message, List<BookDataResponse> books)
    {
        if (books.Count != 0)
        {
            var buttons = CreateUserButtonsForUpdate(books);
            var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
            await _botClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, inlineKeyboardMarkup);
        }
        else
        {
            await _botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
        }
    }

    public async Task UpdateBookButtonsAndMessageTextAsync(long chatId, int messageId, string messageText, List<BookDataResponse> books, bool firstPage, ChatState chatState)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtonsAsync(books, firstPage);
        var inlineKeyboardMarkup = SetInlineKeyboardInColumn(buttons);
        await _botClient.EditMessageTextAsync(chatId, messageId, messageText, replyMarkup: inlineKeyboardMarkup);
    }

    public List<InlineKeyboardButton> CreateBookButtonsAsync(List<BookDataResponse> books, bool firstPage)
    {
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        foreach (BookDataResponse book in books)
        {
            var buttonText = (book.BookReaderId is null ? EmojiNewInSquare : EmojiLock) + $" {book.Title}";
            var callbackData = book.BookReaderId is null ? book.Id.ToString() : "Borrowed";
            var button = InlineKeyboardButton.WithCallbackData(text: buttonText, callbackData: callbackData);
            buttons.Add(button);
        }
        if (firstPage && buttons.Count <= SharePointService.AmountBooks)
        {
        }
        else if (firstPage)
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

        if (books.Count == SharePointService.AmountBooks + 1)
            buttons.RemoveAt(SharePointService.AmountBooks);
        return buttons;
    }


    public async Task DisplayInlineButtonsWithMessageAsync(Message message, string messageText, params string[] buttons)
    {
        var buttonsDict = buttons.ToDictionary(key => key, val => val);
        var inlineButtons = CreateInlineKeyboardMarkup(buttonsDict);
        await _botClient.SendTextMessageAsync(message.Chat.Id, messageText, replyMarkup: inlineButtons);
    }

    public async Task UpdateFilterBooksMessageWithInlineKeyboardAsync(long chatId, int messageId, string[] bookPaths, string message = null)
    {
        var inlineKeyboardMarkup = GetInlineKeybordInTwoColumns(bookPaths);
        if (message is null)
        {
            await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: inlineKeyboardMarkup);
        }
        else
        {
            await _botClient.EditMessageTextAsync(chatId, messageId, message, replyMarkup: inlineKeyboardMarkup);
        }
    }

    public async Task SendFilterBooksMessageWithInlineKeyboardAsync(long chatId, string message, string[] bookPaths)
    {
        var inlineKeyboardMarkup = GetInlineKeybordInTwoColumns(bookPaths);
        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: inlineKeyboardMarkup);
    }

    public async Task SendLibraryMenuMessageAsync(long chatId)
    {
        var replyMarkup = GetLibraryMenuMarkup();
        var message = $"Welcome to `Library` menu{Environment.NewLine}" +
                      $"`Search books` - search all books in library by name{Environment.NewLine}" +
                      $"`Filter by path` - show books filtered by chosen paths{Environment.NewLine}" +
                      $"`Show all books` - show all books in library";

        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup, parseMode: ParseMode.Markdown);
    }

    public async Task SendFilterMenuMessageWithKeyboardAsync(long chatId)
    {
        var replyKeyboard = GetFilterMenuKeyboard();
        var message = $"Welcome to `Filter by path` menu{Environment.NewLine}" +
                      $"`Clear filters` - send you new message without added filters{Environment.NewLine}" +
                      $"`Show filtered` - show books filtered by chosen paths";

        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyKeyboard, parseMode: ParseMode.Markdown);
    }

    public async Task SendHelpMenuAsync(long chatId)
    {
        var replyMarkup = GetHelpMenuMarkup();
        var message = $"Welcome to `Help` menu{Environment.NewLine}" +
                      $"`About` - show info about bot and its actual version{Environment.NewLine}" +
                      $"`Feedback` - send feedback about your user experience or suggest new features";

        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup, parseMode: ParseMode.Markdown);
    }

    public async Task SendTextMessageAsync(long chatId, string message)
    {
        await _botClient.SendTextMessageAsync(chatId, message);
    }

    public async Task SendFeedbackMenuAsync(long chatId)
    {
        var replyMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Cancel" } })
        {
            ResizeKeyboard = true
        };

        var message = $"Please enter your feedback";
        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup);
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

    private ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Library", "My books" },
            new KeyboardButton[] { "Help" }
        })
        {
            ResizeKeyboard = true
        };
    }

    private ReplyKeyboardMarkup GetHelpMenuMarkup()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "About", "Feedback" },
            new KeyboardButton[] { "Cancel"}
        })
        {
            ResizeKeyboard = true
        };
    }

    private ReplyKeyboardMarkup GetLibraryMenuMarkup()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Search books", "Filter by path" },
            new KeyboardButton[] { "Show all books" },
            new KeyboardButton[] { "Cancel"}
        })
        {
            ResizeKeyboard = true
        };
    }

    private ReplyKeyboardMarkup GetFilterMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Clear filters", "Show filtered" },
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