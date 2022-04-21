using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;

namespace LibBot.Services
{
    public class SharePointService : ISharePointService
    {
        private readonly IHttpClientFactory _clientFactory;

        public SharePointService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<bool> IsUserExistInSharePoint(string login)
        {
            var client = _clientFactory.CreateClient("SharePoint");
            var httpResponse = await client.GetAsync($"_api/web/siteusers?$filter=Title eq '{login}'&$select=Email");
            if (!httpResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var contentsString = await httpResponse.Content.ReadAsStringAsync();
            var userData = JsonConvert.DeserializeObject<UserDataResponse>(contentsString);
            return userData?.Email != null && userData.Email.Contains(login, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
