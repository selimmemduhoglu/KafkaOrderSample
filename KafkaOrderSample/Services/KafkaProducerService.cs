using Confluent.Kafka;
using KafkaOrderSample.Infrastructure;
using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Models;
using KafkaOrderSample.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KafkaOrderSample.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private bool _disposed = false;

    public KafkaProducerService(IOptions<KafkaConfiguration> kafkaConfig, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = kafkaConfig.Value.GetProducerConfig();
        _producer = new ProducerBuilder<string, string>(config)
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
                _logger.Log(logLevel, $"Kafka Producer: {message.Message}");
            })
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError($"Kafka Producer Error: {e.Reason}. Is Fatal: {e.IsFatal}");
            })
            .Build();

        _logger.LogInformation("Kafka Producer Service initialized");
    }

    public async Task<DeliveryResult<string, string>> ProduceAsync(string topic, string key, string value)
    {
        try
        {
            _logger.LogDebug($"Producing message to topic {topic}, key: {key}");

            Message<string, string> message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "source", System.Text.Encoding.UTF8.GetBytes("orders-api") },
                    { "created", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")) }
                }
            };

            DeliveryResult<string, string> result = await _producer.ProduceAsync(topic, message);

            _logger.LogInformation($"Message delivered to topic {result.Topic}, partition {result.Partition}, offset {result.Offset}");

            return result;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError($"Failed to deliver message to topic {topic}: {ex.Error.Reason}");
            throw;
        }
    }

    public async Task<bool> SendOrderAsync(Order order, string topic = KafkaTopics.NewOrders)
    {
        try
        {
            string orderJson = JsonSerializer.Serialize(order, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await ProduceAsync(topic, order.Id.ToString(), orderJson);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending order {order.Id} to Kafka");
            return false;
        }
    }

    public async Task<bool> SendOrderStatusAsync(Guid orderId, OrderStatus status, string notes = null)
    {
        try
        {
            var statusUpdate = new OrderStatusDto
            {
                OrderId = orderId,
                Status = status.ToString(),
                LastUpdated = DateTime.UtcNow,
                Notes = notes
            };

            var statusJson = JsonSerializer.Serialize(statusUpdate, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await ProduceAsync(KafkaTopics.OrderStatus, orderId.ToString(), statusJson);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending status update for order {orderId} to Kafka");
            return false;
        }
    }

    public void Flush(TimeSpan timeout)
    {
        _producer.Flush(timeout);
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
                _producer.Flush(TimeSpan.FromSeconds(5));
                _producer.Dispose();
                _logger.LogInformation("Kafka Producer disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Kafka Producer");
            }
        }

        _disposed = true;
    }
}
