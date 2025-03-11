namespace KafkaOrderSample.Models;

public class Order
{
	public Guid Id { get; set; }
	public string CustomerName { get; set; }
	public string CustomerEmail { get; set; }
	public DateTime OrderDate { get; set; }
	public OrderStatus Status { get; set; }
	public List<OrderItem> Items { get; set; }
	public decimal TotalAmount { get; set; }
	public string ShippingAddress { get; set; }
	public string TrackingNumber { get; set; }
	public DateTime? ProcessedDate { get; set; }
	public DateTime? ShippedDate { get; set; }
	public DateTime? DeliveredDate { get; set; }
	public string Notes { get; set; }
}
