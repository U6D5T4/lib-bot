﻿using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace LibBot.Services;

public class CodeDbService:ICodeDbService
{
    private ResourceManager _resourceReader;
    private readonly IAuthDbService _authDbService;
    private readonly IHttpClientFactory _httpClientFactory;
    public CodeDbService(IAuthDbService authDbService, IHttpClientFactory httpClientFactory)
    {
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        _authDbService = authDbService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task CreateItemAsync(CodeDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Firebase");
        var uri = client.BaseAddress + _resourceReader.GetString("Code_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        var res = await client.PutAsync(uri, JsonContent.Create(item));
    }

    public async Task<CodeDbModel> ReadItemAsync(long chatId) 
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Firebase");
        var uri = client.BaseAddress + _resourceReader.GetString("Code_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        var responce = await client.GetAsync(uri);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<CodeDbModel>(stringResponce);
        return data;
    }

    public async Task UpdateItemAsync(CodeDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Firebase");
        var uri = client.BaseAddress + _resourceReader.GetString("Code_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        await client.PatchAsync(uri, JsonContent.Create(item));
    }

    public async Task DeleteItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Firebase");
        var uri = client.BaseAddress + _resourceReader.GetString("Code_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        await client.DeleteAsync(uri);
    }
}
