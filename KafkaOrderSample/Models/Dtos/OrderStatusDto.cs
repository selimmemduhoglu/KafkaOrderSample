namespace KafkaOrderSample.Models.Dtos;

public class OrderStatusDto
{
	public Guid OrderId { get; set; }
	public string Status { get; set; }
	public DateTime LastUpdated { get; set; }
	public string TrackingNumber { get; set; }
	public string Notes { get; set; }
}
