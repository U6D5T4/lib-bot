using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services;

public class BookService: IBookService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMessageService _messageService;
    private static InlineKeyboardMarkup InlineKeyboard;

    public BookService(ITelegramBotClient botClient, IMessageService messageService)
    {
        _botClient = botClient;
        _messageService = messageService;
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

    public async Task<Message> DisplayBookButtons(ITelegramBotClient bot, Message message, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books);
        InlineKeyboard = _messageService.SetInlineKeyboardInTwoColumns(buttons);
        return await _botClient.SendTextMessageAsync(message.Chat.Id, "These books are in our library.", replyMarkup: InlineKeyboard);
    }

    public async Task UpdateBookButtons(ITelegramBotClient bot, Message message, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books);
        InlineKeyboard = _messageService.SetInlineKeyboardInTwoColumns(buttons);
        await _botClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, InlineKeyboard);
    }
}
