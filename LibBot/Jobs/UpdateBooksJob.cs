using LibBot.Models.SharePointResponses;
using Quartz;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LibBot.Jobs;

class UpdateBooksJob : BooksStorage, IJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    public UpdateBooksJob(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    public async Task<Task> Execute(IJobExecutionContext context)
    {

        Books = await GetAllBooksFromSharePointAsync();   
        return Task.CompletedTask;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await Execute(context);
    }

    private async Task<List<BookDataResponse>> GetAllBooksFromSharePointAsync()
    {
        var client = _httpClientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,BookReaderId,TakenToRead,Technology&$top=300&$orderby=Title");

        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        if (result.Count == 0)
            result = new List<BookDataResponse>();

        return result;
    }
}