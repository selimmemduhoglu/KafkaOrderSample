

using KafkaOrderSample.Infrastructure;
using KafkaOrderSample.Infrastructure.Repositories;
using KafkaOrderSample.Services;
using KafkaOrderSample.Services.Interfaces;
using Microsoft.OpenApi.Models;

namespace KafkaOrdersApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			// Register configurations
			builder.Services.Configure<KafkaConfiguration>(builder.Configuration.GetSection("Kafka"));

			// Register repositories
			builder.Services.AddSingleton<IOrderRepository, OrderRepository>();

			// Register services
			builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
			builder.Services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();
			builder.Services.AddScoped<IOrderService, OrderService>();

			// Register background services
			builder.Services.AddHostedService<KafkaConsumerHostedService>(); // Singleton olarak çalışıyor.

			builder.Services.AddControllers();

			// Add Swagger
			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Kafka Orders API", Version = "v1" });
			});

			// Add CORS
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyOrigin()
						  .AllowAnyMethod()
						  .AllowAnyHeader();
				});
			});

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kafka Orders API v1"));
			}

			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseCors("AllowAll");
			app.UseAuthorization();

			app.MapControllers(); // Modern kullaným

			app.Run();
		}
	}
}
