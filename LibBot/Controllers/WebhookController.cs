using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LibBot.Services.Interfaces;
using Telegram.Bot.Types;

namespace LibBot.Controllers;

public class WebhookController : ControllerBase
{
    private static readonly NLog.Logger _logger;
    static WebhookController() => _logger = NLog.LogManager.GetCurrentClassLogger();

    [HttpPost]
    public async Task<IActionResult> Post([FromServices] IHandleUpdateService handleUpdateService,
                                          [FromBody] Update update)
    {
        if (update is null)
        {
            _logger.Warn($"{nameof(Update)} from telegram Api was null");
            return BadRequest();
        }

        await handleUpdateService.HandleAsync(update);
        return Ok();
    }
}
