using Confluent.Kafka;

namespace KafkaOrderSample.Infrastructure;

public class KafkaConfiguration
{
	public string BootstrapServers { get; set; }
	public string GroupId { get; set; }
	public bool EnableAutoCommit { get; set; }
	public int AutoCommitIntervalMs { get; set; }
	public string AutoOffsetReset { get; set; }
	public int SessionTimeoutMs { get; set; }
	public int StatisticsIntervalMs { get; set; }
	public ProducerConfig GetProducerConfig()
	{
		return new ProducerConfig
		{
			BootstrapServers = BootstrapServers,
			// İdempotent producer, exactly-once delivery garanti eder
			EnableIdempotence = true,
			// Yüksek güvenilirlik için ack tüm replica'lardan gelsin
			Acks = Acks.All,
			// Retry ayarları
			MessageSendMaxRetries = 3,
			RetryBackoffMs = 1000,
			// Linger.ms, daha yüksek verim için mesajları batch olarak göndermek için bekleme süresi
			LingerMs = 5,
			// Mesaj sıkıştırma
			CompressionType = CompressionType.Snappy
		};
	}

	public ConsumerConfig GetConsumerConfig()
	{
		return new ConsumerConfig
		{
			BootstrapServers = BootstrapServers,
			GroupId = GroupId,
			EnableAutoCommit = EnableAutoCommit,
			AutoCommitIntervalMs = AutoCommitIntervalMs,
			AutoOffsetReset = Enum.Parse<AutoOffsetReset>(AutoOffsetReset),
			SessionTimeoutMs = SessionTimeoutMs,
			StatisticsIntervalMs = StatisticsIntervalMs,
			// Consumer performansını artırmak için
			FetchMaxBytes = 5242880, // 5MB
			MaxPartitionFetchBytes = 1048576 // 1MB
		};
	}
}
