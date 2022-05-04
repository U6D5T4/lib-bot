using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System;
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

    public ReplyKeyboardMarkup CreateReplyKeyboardMarkup(params string[] nameButtons)
    {
        List<KeyboardButton> buttons = new List<KeyboardButton>();

        foreach (string button in nameButtons)
        {
            buttons.Add(new KeyboardButton(button));
        }

        var replyButtons =  new ReplyKeyboardMarkup(buttons);
        replyButtons.ResizeKeyboard = true;

        return replyButtons;
    }

    public  InlineKeyboardMarkup CreateInlineKeyboardMarkup(params string[] nameButtons)
    {
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        foreach(string button in nameButtons)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: button, callbackData: button));
        }
        
        return new InlineKeyboardMarkup(buttons);
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
    public async Task<Message> SayDefaultMessageAsync(ITelegramBotClient bot, Message message)
    {
        return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hey, I'm LibBot. If you are seeing this message, You have completed authentication successfully!", replyMarkup: CreateReplyKeyboardMarkup("All Books", "My Books", "Search Books"));
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

    public async Task CreateYesAndNoButtons(CallbackQuery callbackQuery, string message)
    {
        var inlineKeyboard = CreateInlineKeyboardMarkup("No","Yes");
        await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, message);
        await _botClient.EditMessageReplyMarkupAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, replyMarkup: inlineKeyboard);
    }

    public async Task<Message> DisplayBookButtons(Message message, string messageText, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books);
        var inlineKeyboardMarkup = SetInlineKeyboardInTwoColumns(buttons);
        return await _botClient.SendTextMessageAsync(message.Chat.Id, messageText, replyMarkup: inlineKeyboardMarkup);
    }

    public async Task UpdateBookButtons(Message message, List<BookDataResponse> books)
    {
        List<InlineKeyboardButton> buttons = CreateBookButtons(books);
        var inlineKeyboardMarkup = SetInlineKeyboardInTwoColumns(buttons);
        await _botClient.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, inlineKeyboardMarkup);
    }

    public List<InlineKeyboardButton> CreateBookButtons(List<BookDataResponse> books)
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
                buttonText = book.BookReaderId is null ? book.Title : "(borrowed)" + book.Title;
            }
            var callbackData = book.BookReaderId is null ? book.Id.ToString() : "Borrowed";
            var button = InlineKeyboardButton.WithCallbackData(text: buttonText, callbackData: callbackData);
            buttons.Add(button);
        }

        if (buttons.Count == SharePointService.AmountBooks)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Previous", callbackData: "Previous"));
            buttons.Add(InlineKeyboardButton.WithCallbackData(text: "Next", callbackData: "Next"));
        }

        return buttons;
    }
}