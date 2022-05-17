using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LibBot.Services.Interfaces;
using Telegram.Bot.Types;
using System.Resources;
using System.Reflection;
using System;

namespace LibBot.Controllers;

public class WebhookController : ControllerBase
{
    private static readonly NLog.Logger _logger;
    private static ResourceManager _resourceReader;
    static WebhookController()
        {
          _logger = NLog.LogManager.GetCurrentClassLogger();
          _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        }   

    [HttpPost]
    public async Task<IActionResult> Post([FromServices] IHandleUpdateService handleUpdateService,
                                          [FromBody] Update update)
    {
        if (update is null)
        {
            _logger.Warn(String.Format(_resourceReader.GetString("LogWrongTelegramUpdate"), nameof(Update)));
            return BadRequest();
        }

        await handleUpdateService.HandleAsync(update);
        return Ok();
    }
}
