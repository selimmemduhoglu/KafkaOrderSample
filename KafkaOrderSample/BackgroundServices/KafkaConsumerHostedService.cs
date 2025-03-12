using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Models;
using KafkaOrderSample.Services.Interfaces;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly IKafkaConsumerService _kafkaConsumerService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<KafkaConsumerHostedService> _logger;

    public KafkaConsumerHostedService(
        IKafkaConsumerService kafkaConsumerService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<KafkaConsumerHostedService> logger)
    {
        _kafkaConsumerService = kafkaConsumerService;
        _serviceScopeFactory = serviceScopeFactory;
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

    private void OnOrderReceived(object sender, Order order)
    {
        try
        {
            _logger.LogInformation($"Processing order received from Kafka: {order.Id}");

            Task.Run(async () =>
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IOrderService orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                try
                {
                    await orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing,
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

    private void OnStatusUpdateReceived(object sender, OrderStatusDto statusUpdate)
    {
        try
        {
            _logger.LogInformation($"Processing status update from Kafka: Order {statusUpdate.OrderId} -> {statusUpdate.Status}");

            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                try
                {
                    if (Enum.TryParse<OrderStatus>(statusUpdate.Status, out var orderStatus))
                    {
                        await orderService.UpdateOrderStatusAsync(statusUpdate.OrderId, orderStatus, statusUpdate.Notes);
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
    }
}
