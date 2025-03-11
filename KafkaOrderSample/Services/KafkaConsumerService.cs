using Confluent.Kafka;
using KafkaOrderSample.Infrastructure;
using KafkaOrderSample.Models;
using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KafkaOrderSample.Services;

public class KafkaConsumerService : IKafkaConsumerService, IDisposable
{
	private readonly IConsumer<string, string> _consumer;
	private readonly ILogger<KafkaConsumerService> _logger;
	private CancellationTokenSource _cancellationTokenSource;
	private Task _consumerTask;
	private bool _disposed = false;

	public event EventHandler<Order> OrderReceived;
	public event EventHandler<OrderStatusDto> StatusUpdateReceived;
	public event EventHandler<string> ConsumerErrorOccurred;

	public KafkaConsumerService(IOptions<KafkaConfiguration> kafkaConfig, ILogger<KafkaConsumerService> logger)
	{
		_logger = logger;
		var config = kafkaConfig.Value.GetConsumerConfig();

		_consumer = new ConsumerBuilder<string, string>(config)
			.SetLogHandler((_, message) =>
			{
				LogLevel logLevel = message.Level switch
				{
					SyslogLevel.Emergency => LogLevel.Critical,
					SyslogLevel.Alert => LogLevel.Critical,
					SyslogLevel.Critical => LogLevel.Critical,
					SyslogLevel.Error => LogLevel.Error,
					SyslogLevel.Warning => LogLevel.Warning,
					SyslogLevel.Notice => LogLevel.Information,
					SyslogLevel.Info => LogLevel.Information,
					SyslogLevel.Debug => LogLevel.Debug,
					_ => LogLevel.Information
				};
				_logger.Log(logLevel, $"Kafka Consumer: {message.Message}");
			})
			.SetErrorHandler((_, e) =>
			{
				_logger.LogError($"Kafka Consumer Error: {e.Reason}. Is Fatal: {e.IsFatal}");
				ConsumerErrorOccurred?.Invoke(this, $"Error: {e.Reason}");
			})
			.SetPartitionsAssignedHandler((c, partitions) =>
			{
				_logger.LogInformation($"Assigned partitions: [{string.Join(", ", partitions)}]");
			})
			.SetPartitionsRevokedHandler((c, partitions) =>
			{
				_logger.LogInformation($"Revoked partitions: [{string.Join(", ", partitions)}]");
			})
			.Build();

		_logger.LogInformation("Kafka Consumer Service initialized");
	}

	public async Task StartConsumingAsync(CancellationToken cancellationToken)
	{
		_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		// Subscribe to multiple topics
		_consumer.Subscribe(new[] { KafkaTopics.NewOrders, KafkaTopics.OrderStatus });

		_logger.LogInformation("Kafka Consumer subscribed to topics and started consuming");

		_consumerTask = Task.Run(async () =>
		{
			try
			{
				while (!_cancellationTokenSource.Token.IsCancellationRequested)
				{
					try
					{
						var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);

						if (consumeResult != null)
						{
							await ProcessMessageAsync(consumeResult);
						}
					}
					catch (ConsumeException ex)
					{
						_logger.LogError($"Consume error: {ex.Error.Reason}");
						ConsumerErrorOccurred?.Invoke(this, $"Consume error: {ex.Error.Reason}");
					}
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Kafka consumption was cancelled");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error during Kafka consumption");
				ConsumerErrorOccurred?.Invoke(this, $"Unexpected error: {ex.Message}");
			}
			finally
			{
				try
				{
					_consumer.Close();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error closing consumer");
				}
			}
		}, _cancellationTokenSource.Token);

		await Task.CompletedTask;
	}

	private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult)
	{
		_logger.LogDebug($"Processing message: Topic: {consumeResult.Topic}, Partition: {consumeResult.Partition}, Offset: {consumeResult.Offset}");

		try
		{
			switch (consumeResult.Topic)
			{
				case KafkaTopics.NewOrders:
					var order = JsonSerializer.Deserialize<Order>(consumeResult.Message.Value,
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					_logger.LogInformation($"Received new order {order.Id} for customer {order.CustomerName}");
					OrderReceived?.Invoke(this, order);
					break;

				case KafkaTopics.OrderStatus:
					var statusUpdate = JsonSerializer.Deserialize<OrderStatusDto>(consumeResult.Message.Value,
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					_logger.LogInformation($"Received status update for order {statusUpdate.OrderId}: {statusUpdate.Status}");
					StatusUpdateReceived?.Invoke(this, statusUpdate);
					break;

				default:
					_logger.LogWarning($"Received message from unhandled topic: {consumeResult.Topic}");
					break;
			}
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, $"Error deserializing message from topic {consumeResult.Topic}: {consumeResult.Message.Value}");
		}

		await Task.CompletedTask;
	}

	public async Task StopConsumingAsync()
	{
		_logger.LogInformation("Stopping Kafka consumer...");

		if (_cancellationTokenSource != null)
		{
			_cancellationTokenSource.Cancel();

			if (_consumerTask != null)
			{
				try
				{
					await _consumerTask;
				}
				catch (OperationCanceledException)
				{
					_logger.LogInformation("Consumer task was cancelled");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error waiting for consumer task to complete");
				}
			}
		}

		_logger.LogInformation("Kafka consumer stopped");
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			try
			{
				_consumer?.Close();
				_consumer?.Dispose();
				_cancellationTokenSource?.Dispose();
				_logger.LogInformation("Kafka Consumer disposed");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing Kafka Consumer");
			}
		}

		_disposed = true;
	}
}
