// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
using System.Globalization;
using System.Text;

namespace LuckyPackWebApi.Models;

public class Order
{
    public DateTime OrderDate { get; set; }
    
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public int TotalItems { get; set; }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append("Дата заказа ").AppendLine(OrderDate.ToString(CultureInfo.InvariantCulture));
        sb.Append("Сумма итого ").AppendLine(Total.ToString(CultureInfo.InvariantCulture));
        
        //TODO: количество в чём?
        sb.Append("Кол-во товаров ").AppendLine(TotalItems.ToString(CultureInfo.InvariantCulture));
        
        return sb.ToString();
    }
}