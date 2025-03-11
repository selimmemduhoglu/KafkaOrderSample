namespace KafkaOrderSample.Models;

public enum OrderStatus
{
	Created = 0,
	Processing = 1,
	Shipped = 2,
	Delivered = 3,
	Cancelled = 4,
	Failed = 5
}