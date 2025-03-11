using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Models;

namespace KafkaOrderSample.Services.Interfaces;

public interface IKafkaConsumerService
{
	Task StartConsumingAsync(CancellationToken cancellationToken);
	Task StopConsumingAsync();
	event EventHandler<Order> OrderReceived;
	event EventHandler<OrderStatusDto> StatusUpdateReceived;
	event EventHandler<string> ConsumerErrorOccurred;
}
