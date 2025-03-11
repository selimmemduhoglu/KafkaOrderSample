using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Models;

namespace KafkaOrderSample.Services.Interfaces;

public interface IOrderService
{
	Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto);
	Task<OrderDto> GetOrderByIdAsync(Guid id);
	Task<OrderStatusDto> GetOrderStatusAsync(Guid id);
	Task<OrderDto> UpdateOrderStatusAsync(Guid id, OrderStatus status, string notes = null);
	Task<IEnumerable<OrderDto>> GetRecentOrdersAsync(int count = 10);
}
