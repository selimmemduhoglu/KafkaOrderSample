using KafkaOrderSample.Models;

namespace KafkaOrderSample.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
	private readonly List<Order> _orders = new List<Order>();
	private readonly ILogger<OrderRepository> _logger;
	private readonly object _lock = new object();

	public OrderRepository(ILogger<OrderRepository> logger)
	{
		_logger = logger;
	}

	public Task<Order> GetByIdAsync(Guid id)
	{
		_logger.LogDebug($"Getting order with id {id}");

		lock (_lock)
		{
			var order = _orders.FirstOrDefault(o => o.Id == id);
			return Task.FromResult(order);
		}
	}

	public Task<IEnumerable<Order>> GetRecentOrdersAsync(int count)
	{
		_logger.LogDebug($"Getting {count} recent orders");

		lock (_lock)
		{
			var recentOrders = _orders
				.OrderByDescending(o => o.OrderDate)
				.Take(count)
				.ToList();

			return Task.FromResult<IEnumerable<Order>>(recentOrders);
		}
	}

	public Task<Order> AddAsync(Order order)
	{
		throw new NotImplementedException();
	}

	public Task<bool> DeleteAsync(Guid id)
	{
		throw new NotImplementedException();
	}


	

	public Task<bool> OrderExistsAsync(Guid id)
	{
		throw new NotImplementedException();
	}

	public Task<Order> UpdateAsync(Order order)
	{
		throw new NotImplementedException();
	}
}
