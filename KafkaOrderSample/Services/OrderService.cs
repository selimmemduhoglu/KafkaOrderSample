using KafkaOrderSample.Infrastructure.Repositories;
using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Models;
using KafkaOrderSample.Services.Interfaces;

namespace KafkaOrderSample.Services;

public class OrderService : IOrderService
{
	private readonly IOrderRepository _orderRepository;
	private readonly IKafkaProducerService _kafkaProducer;
	private readonly ILogger<OrderService> _logger;

	public OrderService(
		IOrderRepository orderRepository,
		IKafkaProducerService kafkaProducer,
		ILogger<OrderService> logger)
	{
		_orderRepository = orderRepository;
		_kafkaProducer = kafkaProducer;
		_logger = logger;
	}

	public async Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto)
	{
		// Model doğrulama işlemlerini burada da yapabilirsiniz

		var order = new Order
		{
			Id = Guid.NewGuid(),
			CustomerName = orderDto.CustomerName,
			CustomerEmail = orderDto.CustomerEmail,
			OrderDate = DateTime.UtcNow,
			Status = OrderStatus.Created,
			ShippingAddress = orderDto.ShippingAddress,
			Notes = orderDto.Notes,
			Items = orderDto.Items.Select(item => new OrderItem
			{
				Id = Guid.NewGuid(),
				ProductId = item.ProductId,
				ProductName = item.ProductName,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice
			}).ToList()
		};

		// Toplam tutarı hesapla
		order.TotalAmount = order.Items.Sum(item => item.Quantity * item.UnitPrice);

		// Siparişi veritabanına kaydet
		var savedOrder = await _orderRepository.AddAsync(order);

		// Siparişi Kafka'ya gönder
		var sent = await _kafkaProducer.SendOrderAsync(savedOrder);

		if (!sent)
		{
			_logger.LogWarning($"Order {savedOrder.Id} was saved to database but failed to publish to Kafka");
		}

		// DTO'ya dönüştür ve dön
		return MapOrderToDto(savedOrder);
	}

	public async Task<OrderDto> GetOrderByIdAsync(Guid id)
	{
		var order = await _orderRepository.GetByIdAsync(id);

		if (order == null)
		{
			return null;
		}

		return MapOrderToDto(order);
	}

	public async Task<OrderStatusDto> GetOrderStatusAsync(Guid id)
	{
		var order = await _orderRepository.GetByIdAsync(id);

		if (order == null)
		{
			return null;
		}

		return new OrderStatusDto
		{
			OrderId = order.Id,
			Status = order.Status.ToString(),
			LastUpdated = GetLastUpdateDate(order),
			TrackingNumber = order.TrackingNumber,
			Notes = order.Notes
		};
	}

	public async Task<OrderDto> UpdateOrderStatusAsync(Guid id, OrderStatus status, string notes = null)
	{
		var order = await _orderRepository.GetByIdAsync(id);

		if (order == null)
		{
			return null;
		}

		// Durum güncellemesi
		order.Status = status;

		// Notes güncellemesi
		if (!string.IsNullOrEmpty(notes))
		{
			order.Notes = notes;
		}

		// Duruma göre tarih bilgilerini güncelle
		switch (status)
		{
			case OrderStatus.Processing:
				order.ProcessedDate = DateTime.UtcNow;
				break;
			case OrderStatus.Shipped:
				order.ShippedDate = DateTime.UtcNow;
				// Gerçek bir uygulamada takip numarası oluşturulabilir
				if (string.IsNullOrEmpty(order.TrackingNumber))
				{
					order.TrackingNumber = $"TRK-{DateTime.UtcNow.Ticks % 10000000:D7}";
				}
				break;
			case OrderStatus.Delivered:
				order.DeliveredDate = DateTime.UtcNow;
				break;
		}

		// Siparişi güncelle
		var updatedOrder = await _orderRepository.UpdateAsync(order);

		// Durum güncellemesini Kafka'ya gönder
		await _kafkaProducer.SendOrderStatusAsync(updatedOrder.Id, updatedOrder.Status, updatedOrder.Notes);

		// DTO'ya dönüştür ve dön
		return MapOrderToDto(updatedOrder);
	}

	public async Task<IEnumerable<OrderDto>> GetRecentOrdersAsync(int count = 10)
	{
		var orders = await _orderRepository.GetRecentOrdersAsync(count);
		return orders.Select(MapOrderToDto);
	}

	private DateTime GetLastUpdateDate(Order order)
	{
		// Sipariş durumuna göre en son güncelleme tarihini belirle
		return order.Status switch
		{
			OrderStatus.Delivered => order.DeliveredDate ?? order.OrderDate,
			OrderStatus.Shipped => order.ShippedDate ?? order.OrderDate,
			OrderStatus.Processing => order.ProcessedDate ?? order.OrderDate,
			_ => order.OrderDate
		};
	}

	private OrderDto MapOrderToDto(Order order)
	{
		return new OrderDto
		{
			Id = order.Id,
			CustomerName = order.CustomerName,
			CustomerEmail = order.CustomerEmail,
			OrderDate = order.OrderDate,
			Status = order.Status.ToString(),
			TotalAmount = order.TotalAmount,
			ShippingAddress = order.ShippingAddress,
			TrackingNumber = order.TrackingNumber,
			ProcessedDate = order.ProcessedDate,
			ShippedDate = order.ShippedDate,
			DeliveredDate = order.DeliveredDate,
			Notes = order.Notes,
			Items = order.Items.Select(item => new OrderItemDto
			{
				ProductId = item.ProductId,
				ProductName = item.ProductName,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice
			}).ToList()
		};
	}
}
