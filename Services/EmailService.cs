using System.Net;
using System.Text;
using LuckyPackWebApi.Models;
using LuckyPackWebApi.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LuckyPackWebApi.Services;

public class EmailService(
    IOptions<EmailOptions> settings,
    ILogger<EmailService> logger
)
    : IEmailService
{
    private readonly EmailOptions _options = settings.Value;
    
    public async Task<bool> SendOrderEmailAsync(
        Order order,
        TelegramUser user,
        CancellationToken ct = default
    )
    {
        try
        {
            var from = $"{user.FirstName} {user.LastName} (@{user.Username})";
            
            var subject = $"Новый заказ от {from}";
            var body = CreateBody(order, user);
            
            return await SendEmailAsync(
                subject,
                body,
                true,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending order email");
            return false;
        }
    }
    
    public async Task<bool> SendEmailAsync(
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken ct = default
    )
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Создание Smtp-клиента...");
            
            //Создание клиента
            using var client = new SmtpClient();
            
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Smtp-клиент создан. Попытка соединения...");
            
            //Коннект
            await client.ConnectAsync(
                _options.SmtpServer,
                _options.SmtpPort,
                _options.EnableSsl,
                ct
            );
            
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Успешное соединение. Попытка аутентификации...");
            
            //Аутентификация
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(
                _options.Email,
                _options.Password,
                ct
            );
            
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Успешная аутентификация. Создание и отправка сообщения...");
            
            //Создание письма
            var mail = CreateMail(subject, body);
            
            //Отправка письма
            await client.SendAsync(mail, ct);
            
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Сообщение отправлено.");
            
            //Отключение
            await client.DisconnectAsync(true, ct);
            
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error sending email: {Subject}", subject);
            return false;
        }
    }
    
    private MimeMessage CreateMail(
        string subject,
        string body
    )
    {
        var mail = new MimeMessage();
        
        mail.From.Add(
            new MailboxAddress(
                null,
                _options.Email
            )
        );
        //TODO: чем sender от from отличается?
        mail.Sender = new MailboxAddress(
            null,
            _options.Email
        );
        
        mail.Subject = subject;
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        mail.Body = bodyBuilder.ToMessageBody();
        
        mail.To.Add(MailboxAddress.Parse(_options.Email));
        return mail;
    }
    
    private static string CreateBody(
        Order order,
        TelegramUser user
    )
    {
        var itemsHtml = new StringBuilder();
        foreach (var item in order.Items)
        {
            itemsHtml.Append(
                $@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>
                        <b>{HtmlEncode(item.Name)}</b>
                    </td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>
                        {item.ProductCode}
                    </td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>
                        {item.Quantity}
                    </td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>
                        {item.Price:N2} ₽
                    </td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>
                        <b>{item.Total:N2} ₽</b>
                    </td>
                </tr>
            ");
        }
        
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #2c3e50; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                .order-info {{ background: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px; }}
                .total {{ font-size: 20px; font-weight: bold; color: #2c3e50; text-align: right; margin-top: 20px; }}
                table {{ width: 100%; border-collapse: collapse; }}
                .footer {{ text-align: center; color: #666; margin-top: 30px; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Новый заказ</h1>
                    <p>{DateTime.Now:dd.MM.yyyy HH:mm}</p>
                </div>
                
                <div class='order-info'>
                    <h3>👤 Информация о клиенте</h3>
                    <p><b>Имя:</b> {HtmlEncode(user.FirstName ?? "Неизвестно")}</p>
                    <p><b>Фамилия:</b> {HtmlEncode(user.LastName ?? "")}</p>
                    <p><b>Username:</b> @{HtmlEncode(user.Username + $"(https://t.me/{user.Username})")}</p>
                    <p><b>ID:</b> {user.Id}</p>
                </div>
                
                <h3>🛒 Состав заказа</h3>
                <table>
                    <thead>
                        <tr style='background: #2c3e50; color: white;'>
                            <th style='padding: 10px;'>Товар</th>
                            <th style='padding: 10px;'>Артикул</th>
                            <th style='padding: 10px;'>Кол-во</th>
                            <th style='padding: 10px;'>Цена</th>
                            <th style='padding: 10px;'>Сумма</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsHtml}
                    </tbody>
                </table>
                
                <div class='total'>
                    Итого: {order.Total:N2} ₽
                </div>
                <div style='text-align: right; color: #666;'>
                    Всего позиций: {order.TotalItems:N0}
                </div>
                
                <div class='footer'>
                    <p>Заказ создан через Telegram WebApp</p>
                </div>
            </div>
        </body>
        </html>
        ";
    }
    
    private static string HtmlEncode(string text)
    {
        return
            string.IsNullOrEmpty(text)
                ? string.Empty
                : WebUtility.HtmlEncode(text);
    }
}