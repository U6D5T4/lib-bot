﻿using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ISharePointService
{
    public Task<bool> IsUserExistInSharePointAsync(string login);
    public Task<string> GetFormDigestValueFromSharePointAsync();
    public Task<UserDataResponse> GetUserDataFromSharePointAsync(string login);
    public Task<bool> ChangeBookStatus(long chatId, int bookId, ChangeBookStatusRequest changeBookStatusRequest);
    public Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber);
    public Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, int userId);
    public Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, string searchQuery);

}