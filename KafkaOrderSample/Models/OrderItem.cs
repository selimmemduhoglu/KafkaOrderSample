namespace KafkaOrderSample.Models;

public class OrderItem
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public string ProductId { get; set; }
	public string ProductName { get; set; }
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public decimal Subtotal => Quantity * UnitPrice;
}
