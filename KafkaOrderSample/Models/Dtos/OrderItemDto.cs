using System.ComponentModel.DataAnnotations;

namespace KafkaOrderSample.Models.Dtos;

public class OrderItemDto
{
	[Required]
	public string ProductId { get; set; }

	[Required]
	public string ProductName { get; set; }

	[Required]
	[Range(1, 100)]
	public int Quantity { get; set; }

	[Required]
	[Range(0.01, 10000)]
	public decimal UnitPrice { get; set; }
}
