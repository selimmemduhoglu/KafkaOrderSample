using KafkaOrderSample.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace KafkaOrdersApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class KafkaMonitorController : ControllerBase
	{
		private readonly ILogger<KafkaMonitorController> _logger;

		public KafkaMonitorController(ILogger<KafkaMonitorController> logger)
		{
			_logger = logger;
		}

		[HttpGet("topics")]
		public IActionResult GetTopics()
		{
			var topics = new List<object>
			{
				new { Name = KafkaTopics.NewOrders, Description = "New orders created by customers" },
				new { Name = KafkaTopics.OrderProcessing, Description = "Orders being processed by the system" },
				new { Name = KafkaTopics.OrderStatus, Description = "Order status updates" },
				new { Name = KafkaTopics.FailedOrders, Description = "Orders that failed processing" }
			};

			return Ok(topics);
		}
	}
}