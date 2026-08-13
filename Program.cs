using LuckyPackWebApi.Background;
using LuckyPackWebApi.Options;
using LuckyPackWebApi.Services;
using Serilog;
using Telegram.Bot;

namespace LuckyPackWebApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        AddSerilog(builder);
        
        ConfigureServices(builder);
        
        var app = builder.Build();
        app.UseSerilogRequestLogging();
        app.UseCors("Development");
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();
        //app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.UseHealthChecks("/health");
        app.Run();
    }
    
    private static void AddSerilog(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration)
        );
    }
    
    private static void ConfigureServices(
        WebApplicationBuilder builder
    )
    {
        var services = builder.Services;
        
        services.AddControllers();
        
        services.AddCors(options =>
        {
            options.AddPolicy(
                "Development",
                policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
        
        //TODO: Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        services.AddOpenApi();
        services.AddScoped<IEmailService, EmailService>();
        services.Configure<EmailOptions>(
            builder.Configuration.GetSection(nameof(EmailOptions))
        );
        services.Configure<TelegramOptions>(
            builder.Configuration.GetSection(nameof(TelegramOptions))
        );
        
        
        var token =
            builder.Configuration["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Telegram bot token is not configured.");
        
        services.AddSingleton<ITelegramBotClient>(_ =>
            new TelegramBotClient(token)
        );
        services.AddHostedService<TelegramBotWorker>();
        
        services.AddHealthChecks();
    }
}