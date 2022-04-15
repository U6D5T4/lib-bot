using LibBot.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LibBot.Services.Interfaces;
using Telegram.Bot.Types;

namespace LibBot.Controllers;

public class WebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromServices] IHandleUpdateService handleUpdateService,
                                          [FromBody] Update update)
    {
        if (update is null)
        {
            return BadRequest();
        }

        await handleUpdateService.SayHelloFromAnton(update);
        await handleUpdateService.SayHelloFromArtyom(update);
        return Ok();
    }
}
