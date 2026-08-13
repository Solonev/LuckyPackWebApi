using System.Text;

namespace LuckyPackWebApi.Models;

public class DataModel
{
    public string Action { get; set; } = string.Empty;
    public Order Order { get; set; } = null!;
    public TelegramUser? User { get; set; } = null!;
    public string? QueryId { get; set; } = null;
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Action: {Action}");
        sb.AppendLine($"QueryId: {QueryId}");
        sb.AppendLine($"Order: {Order}");
        sb.AppendLine($"User: {User}");
        
        return sb.ToString();
    }
}