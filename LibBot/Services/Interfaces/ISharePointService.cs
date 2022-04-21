using System.Threading.Tasks;

namespace LibBot.Services.Interfaces
{
    public interface ISharePointService
    {
        public Task<bool> IsUserExistInSharePoint(string login);
    }
}
