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
		if (order == null)
		{
			throw new ArgumentNullException(nameof(order), "Order cannot be null");
		}

		_logger.LogInformation($"Adding new order {order.Id} for customer {order.CustomerName}");

		lock (_lock)
		{
			_orders.Add(order);
			return Task.FromResult(order);
		}
	}

	public Task<bool> DeleteAsync(Guid id)
	{
		_logger.LogInformation($"Deleting order {id}");

		lock (_lock)
		{
			var order = _orders.FirstOrDefault(o => o.Id == id);
			if (order != null)
			{
				_orders.Remove(order);
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}
	}

	public Task<bool> OrderExistsAsync(Guid id)
	{
		lock (_lock)
		{
			var exists = _orders.Any(o => o.Id == id);
			return Task.FromResult(exists);
		}
	}

	public Task<Order> UpdateAsync(Order order)
	{
		if (order == null)
		{
			throw new ArgumentNullException(nameof(order), "Order cannot be null");
		}

		_logger.LogInformation($"Updating order {order.Id}, new status: {order.Status}");

		lock (_lock)
		{
			var existingOrder = _orders.FirstOrDefault(o => o.Id == order.Id);
			if (existingOrder == null)
			{
				throw new KeyNotFoundException($"Order with ID {order.Id} not found");
			}

			// Remove existing and add updated
			_orders.Remove(existingOrder);
			_orders.Add(order);

			return Task.FromResult(order);
		}
	}
}
