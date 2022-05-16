using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackDbService _feedbackDbService;

    public FeedbackService(IFeedbackDbService feedbackDbService)
    {
        _feedbackDbService = feedbackDbService;
    }

    public async Task SaveFeedbackIntoDb(UserFeedbackDbModel userFeedback)
    {
        await _feedbackDbService.CreateItemAsync(userFeedback);
    }
}
