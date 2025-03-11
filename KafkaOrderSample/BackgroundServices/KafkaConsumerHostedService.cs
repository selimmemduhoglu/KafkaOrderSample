

using KafkaOrderSample.Services.Interfaces;

namespace KafkaOrdersApi.BackgroundServices
{
	public class KafkaConsumerHostedService : BackgroundService
	{
		private readonly IKafkaConsumerService _kafkaConsumerService;
		private readonly IOrderService _orderService;
		private readonly ILogger<KafkaConsumerHostedService> _logger;

		public KafkaConsumerHostedService(
			IKafkaConsumerService kafkaConsumerService,
			IOrderService orderService,
			ILogger<KafkaConsumerHostedService> logger)
		{
			_kafkaConsumerService = kafkaConsumerService;
			_orderService = orderService;
			_logger = logger;

			// Subscribe to events
			_kafkaConsumerService.OrderReceived += OnOrderReceived;
			_kafkaConsumerService.StatusUpdateReceived += OnStatusUpdateReceived;
			_kafkaConsumerService.ConsumerErrorOccurred += OnConsumerErrorOccurred;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Kafka Consumer Hosted Service is starting");

			try
			{
				await _kafkaConsumerService.StartConsumingAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error starting Kafka Consumer");
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Kafka Consumer Hosted Service is stopping");

			// Unsubscribe from events
			_kafkaConsumerService.OrderReceived -= OnOrderReceived;
			_kafkaConsumerService.StatusUpdateReceived -= OnStatusUpdateReceived;
			_kafkaConsumerService.ConsumerErrorOccurred -= OnConsumerErrorOccurred;

			await _kafkaConsumerService.StopConsumingAsync();
			await base.StopAsync(cancellationToken);
		}

		private void OnOrderReceived(object sender, Models.Order order)
		{
			try
			{
				_logger.LogInformation($"Processing order received from Kafka: {order.Id}");

				// Process order logic here
				// For example, you might want to update the order status to Processing
				// and perform some business logic

				// This is running on a background thread, so we'll use Task.Run to not block
				Task.Run(async () =>
				{
					try
					{
						// Example: update order status to Processing after validation
						await _orderService.UpdateOrderStatusAsync(order.Id, Models.OrderStatus.Processing,
							"Order received for processing");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, $"Error processing order {order.Id}");
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling order received event");
			}
		}

		private void OnStatusUpdateReceived(object sender, Models.Dtos.OrderStatusDto statusUpdate)
		{
			try
			{
				_logger.LogInformation($"Processing status update from Kafka: Order {statusUpdate.OrderId} -> {statusUpdate.Status}");

				// Process status update logic here
				// For example, you might want to synchronize this with your database

				Task.Run(async () =>
				{
					try
					{
						if (Enum.TryParse<Models.OrderStatus>(statusUpdate.Status, out var orderStatus))
						{
							await _orderService.UpdateOrderStatusAsync(statusUpdate.OrderId, orderStatus,
								statusUpdate.Notes);
						}
						else
						{
							_logger.LogWarning($"Received invalid order status: {statusUpdate.Status}");
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, $"Error processing status update for order {statusUpdate.OrderId}");
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling status update event");
			}
		}

		private void OnConsumerErrorOccurred(object sender, string errorMessage)
		{
			_logger.LogError($"Kafka Consumer error: {errorMessage}");

			// Implement any error handling logic here
			// For example, you might want to notify administrators or retry operations
		}
	}
}