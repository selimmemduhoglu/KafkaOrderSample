using KafkaOrderSample.Models;

namespace KafkaOrderSample.Infrastructure.Repositories;

public interface IOrderRepository
{
	Task<Order> GetByIdAsync(Guid id);
	Task<IEnumerable<Order>> GetRecentOrdersAsync(int count);
	Task<Order> AddAsync(Order order);
	Task<Order> UpdateAsync(Order order);
	Task<bool> DeleteAsync(Guid id);
	Task<bool> OrderExistsAsync(Guid id);
}
