using Confluent.Kafka;
using KafkaOrderSample.Infrastructure;
using KafkaOrderSample.Models;

namespace KafkaOrderSample.Services.Interfaces;

public interface IKafkaProducerService
{
	Task<DeliveryResult<string, string>> ProduceAsync(string topic, string key, string value);
	Task<bool> SendOrderAsync(Order order, string topic = KafkaTopics.NewOrders);
	Task<bool> SendOrderStatusAsync(Guid orderId, OrderStatus status, string notes = null);
	void Flush(TimeSpan timeout);
}