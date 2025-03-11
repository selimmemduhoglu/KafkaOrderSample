using KafkaOrderSample.Models;
using KafkaOrderSample.Models.Dtos;
using KafkaOrderSample.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KafkaOrdersApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class OrdersController : ControllerBase
	{
		private readonly IOrderService _orderService;
		private readonly ILogger<OrdersController> _logger;

		public OrdersController(
			IOrderService orderService,
			ILogger<OrdersController> logger)
		{
			_orderService = orderService;
			_logger = logger;
		}

		[HttpPost]
		public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createOrderDto)
		{
			try
			{
				_logger.LogInformation($"Creating new order for customer {createOrderDto.CustomerName}");

				var order = await _orderService.CreateOrderAsync(createOrderDto);

				return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating order");
				return StatusCode(500, "An error occurred while creating the order");
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
		{
			try
			{
				_logger.LogInformation($"Getting order {id}");

				var order = await _orderService.GetOrderByIdAsync(id);

				if (order == null)
				{
					return NotFound();
				}

				return Ok(order);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error retrieving order {id}");
				return StatusCode(500, "An error occurred while retrieving the order");
			}
		}

		[HttpGet("{id}/status")]
		public async Task<ActionResult<OrderStatusDto>> GetOrderStatus(Guid id)
		{
			try
			{
				_logger.LogInformation($"Getting status for order {id}");

				var status = await _orderService.GetOrderStatusAsync(id);

				if (status == null)
				{
					return NotFound();
				}

				return Ok(status);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error retrieving order status {id}");
				return StatusCode(500, "An error occurred while retrieving the order status");
			}
		}

		[HttpPut("{id}/status")]
		public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto updateDto)
		{
			try
			{
				_logger.LogInformation($"Updating order {id} status to {updateDto.Status}");

				if (!Enum.TryParse<OrderStatus>(updateDto.Status, true, out var orderStatus))
				{
					return BadRequest($"Invalid order status: {updateDto.Status}");
				}

				var order = await _orderService.UpdateOrderStatusAsync(id, orderStatus, updateDto.Notes);

				if (order == null)
				{
					return NotFound();
				}

				return Ok(order);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating order status {id}");
				return StatusCode(500, "An error occurred while updating the order status");
			}
		}

		[HttpGet("recent")]
		public async Task<ActionResult<IEnumerable<OrderDto>>> GetRecentOrders([FromQuery] int count = 10)
		{
			try
			{
				_logger.LogInformation($"Getting {count} recent orders");

				var orders = await _orderService.GetRecentOrdersAsync(count);

				return Ok(orders);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving recent orders");
				return StatusCode(500, "An error occurred while retrieving recent orders");
			}
		}
	}

	// Add this class to support the update status endpoint
	public class UpdateOrderStatusDto
	{
		public string Status { get; set; }
		public string Notes { get; set; }
	}
}