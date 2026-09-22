namespace MarketWorkplace.Api.Models;

public class Order
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime Date { get; set; }
}
