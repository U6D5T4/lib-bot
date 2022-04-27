using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services;

public class MessageService : IMessageService
{
    private readonly ITelegramBotClient _botClient;

    public MessageService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    private ReplyKeyboardMarkup replyKeyboardMarkup = new(
        new[]
        {
            new KeyboardButton[] { "/first", "/second", "All Books" },
        })
    {
        ResizeKeyboard = true
    };

    InlineKeyboardMarkup inlineKeyboard = new(new[]
    {
        new []
        {
            InlineKeyboardButton.WithCallbackData(text: "No", callbackData: "No"),
            InlineKeyboardButton.WithCallbackData(text: "Yes", callbackData: "Yes"),
        }
    });

    public async Task<Message> SayHelloFromAntonAsync(ITelegramBotClient bot, Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hello, this is Anton's function!", replyMarkup: replyKeyboardMarkup);
    }

    public async Task<Message> SendTextMessageAndClearKeyboardAsync(ITelegramBotClient bot, long chatId, string message)
    {
        return await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardRemove());
    }

    public async Task<Message> SayHelloFromArtyomAsync(ITelegramBotClient bot, Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hello, this is Artyom's function!", replyMarkup: replyKeyboardMarkup);
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

    public async Task EditMessageAfterYesAndNoButtons(ITelegramBotClient bot, CallbackQuery callbackQuery)
    {
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, "These books are in our library.");
    }
    public async Task<Message> SayDefaultMessageAsync(ITelegramBotClient bot, Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hey, I'm LibBot. If you are seeing this message, You have completed authentication successfully!", replyMarkup: replyKeyboardMarkup);
    }
    public InlineKeyboardMarkup SetInlineKeyboardInTwoColumns(List<InlineKeyboardButton> inlineButtons)
    {
        var inlineButtonsTwoColumns = new List<InlineKeyboardButton[]>();
        for (var i = 0; i < inlineButtons.Count; i++)
        {
            if (inlineButtons.Count - 1 == i)
            {
                inlineButtonsTwoColumns.Add(new[] { inlineButtons[i] });
            }
            else
                inlineButtonsTwoColumns.Add(new[] { inlineButtons[i], inlineButtons[i + 1] });
            i++;
        }

        return new InlineKeyboardMarkup(inlineButtonsTwoColumns.ToArray());
    }

    public async Task CreateYesAndNoButtons(ITelegramBotClient bot, CallbackQuery callbackQuery)
    {
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, "Are you sure you want to borrow this book?");
        await _botClient.EditMessageReplyMarkupAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, replyMarkup: inlineKeyboard);
    }

    public async Task<Message> DisplayBookButtons(ITelegramBotClient bot, Message message, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books);
        inlineKeyboard = SetInlineKeyboardInTwoColumns(buttons);
        return await _botClient.SendTextMessageAsync(message.Chat.Id, "These books are in our library.", replyMarkup: inlineKeyboard);
    }

    public async Task UpdateBookButtons(Message message, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books);
        inlineKeyboard = SetInlineKeyboardInTwoColumns(buttons);
        await _botClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, inlineKeyboard);
    }

    public List<InlineKeyboardButton> CreateBookButtons(List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        foreach (BookDataResponse book in books)
        {
            var button = InlineKeyboardButton.WithCallbackData(text: book.Title, callbackData: book.Id.ToString());
            buttons.Add(button);
        }

        buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Previous", callbackData: "Previous"));
        buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Next", callbackData: "Next"));

        return buttons;
    }
}