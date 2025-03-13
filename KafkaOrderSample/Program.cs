

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

            // Register configurationsü
            IConfigurationSection kafkaConfiguration = builder.Configuration.GetSection("Kafka");
            builder.Services.Configure<KafkaConfiguration>(kafkaConfiguration);

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
                app.UseSwaggerUI(options =>
                {
                    // Swagger UI, /swagger/index.html üzerinden eriþilebilir olur.
                    options.RoutePrefix = "swagger";
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Baþlýk v1");
                });
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthorization();

            app.MapControllers(); // Modern kullaným


            // Uygulama ana sayfaya ("/") istek geldiðinde Swagger UI'a yönlendirme yapýyoruz.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/weatherforecast")
                {
                    context.Response.Redirect("/swagger/index.html");
                    return;
                }
                await next();
            });

            app.Run();
        }
    }
}
