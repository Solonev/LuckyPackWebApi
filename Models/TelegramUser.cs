// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
using System.Text.Json.Serialization;

namespace LuckyPackWebApi.Models;

public class TelegramUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return $"{Id} - {FirstName} {LastName} - {Username}";
    }
}