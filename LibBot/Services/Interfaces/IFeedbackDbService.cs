using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IFeedbackDbService
{
    Task CreateItemAsync(UserFeedbackDbModel item);
}
