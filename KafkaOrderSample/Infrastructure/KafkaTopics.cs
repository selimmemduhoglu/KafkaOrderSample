namespace KafkaOrderSample.Infrastructure;

public static class KafkaTopics
{
	public const string NewOrders = "new-orders";
	public const string OrderProcessing = "order-processing";
	public const string OrderStatus = "order-status";
	public const string FailedOrders = "failed-orders";
}