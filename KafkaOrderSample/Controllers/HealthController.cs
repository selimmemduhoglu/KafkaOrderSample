using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KafkaOrdersApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class HealthController : ControllerBase
	{
		private readonly ILogger<HealthController> _logger;

		public HealthController(ILogger<HealthController> logger)
		{
			_logger = logger;
		}

		[HttpGet]
		public IActionResult CheckHealth()
		{
			return Ok(new
			{
				Status = "Healthy",
				Timestamp = DateTime.UtcNow
			});
		}
	}
}