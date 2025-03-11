using System.ComponentModel.DataAnnotations;

namespace KafkaOrderSample.Models.Dtos;

public class CreateOrderDto
{
	[Required]
	public string CustomerName { get; set; }

	[Required]
	[EmailAddress]
	public string CustomerEmail { get; set; }

	[Required]
	public string ShippingAddress { get; set; }

	[Required]
	public List<OrderItemDto> Items { get; set; }

	public string Notes { get; set; }
}
