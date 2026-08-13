// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace LuckyPackWebApi.Models;

public class OrderItem
{
    /// <summary>
    /// Артикул товара
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
}