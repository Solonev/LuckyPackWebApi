using LuckyPackWebApi.Models;

namespace LuckyPackWebApi.Services;

public interface IEmailService
{
    Task<bool> SendOrderEmailAsync(
        Order order,
        TelegramUser user,
        CancellationToken ct = default
    );
    
    Task<bool> SendEmailAsync(
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken ct = default
    );
}