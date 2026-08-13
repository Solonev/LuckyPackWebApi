using LuckyPackWebApi.Models;
using LuckyPackWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuckyPackWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(
    IEmailService emailService,
    ILogger<OrderController> logger
)
    : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Get(
        [FromBody] DataModel data,
        CancellationToken ct
    )
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Получен заказ: {@Data}", data);
        
        if (data.User == null)
        {
            logger.LogInformation("Попытка создания заказ без User");
            return BadRequest("User is null");
        }
        
        var emailSent = await emailService.SendOrderEmailAsync(data.Order, data.User, ct);
        
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Отправка email завершена. Success: {EmailSent}",
                emailSent
            );
        
        return
            emailSent
                ? Ok()
                : BadRequest("Не удалось отправить email");
    }
}