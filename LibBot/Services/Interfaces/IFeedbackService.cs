using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IFeedbackService
{
    Task SaveFeedbackIntoDb(UserFeedbackDbModel userFeedback);
}
