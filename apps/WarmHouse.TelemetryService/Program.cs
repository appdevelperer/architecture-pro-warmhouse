using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarmHouse.TelemetryService.Models;
using WarmHouse.TelemetryService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Регистрация репозитория (аналогично DeviceManagementService)
builder.Services.AddSingleton<TelemetryRepository>();
builder.Services.AddHostedService<TelemetryConsumerService>();
builder.Services.AddHttpClient();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Device Management API",
        Version = "v1",
        Description = "Управление устройствами умного дома (отопление, свет, ворота и др.)"
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => "OK");

app.Run();

// --- Background Service ---
public class TelemetryConsumerService : BackgroundService
{
    private readonly TelemetryRepository _repository;
    private readonly ILogger<TelemetryConsumerService> _logger;

    public TelemetryConsumerService(TelemetryRepository repository, ILogger<TelemetryConsumerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",      // ← имя сервиса в docker-compose
            Port = 5672,
            UserName = "admin",
            Password = "password123"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Объявляем тот же exchange, что и в publisher
        channel.ExchangeDeclare(exchange: "telemetry.events", type: ExchangeType.Fanout, durable: true);
        var queueName = channel.QueueDeclare(durable: true).QueueName;
        channel.QueueBind(queue: queueName, exchange: "telemetry.events", routingKey: "");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<DeviceTelemetryEvent>(body);

                if (message != null)
                {
                    var record = new TelemetryRecord
                    {
                        DeviceId = message.DeviceId,
                        IsOn = message.IsOn,
                        Location = message.Location,
                        Type = message.Type,
                        Timestamp = message.Timestamp
                    };

                    await _repository.AddAsync(record);
                    _logger.LogInformation("Saved telemetry for device {DeviceId}", message.DeviceId);
                }

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry message");
                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        // Ожидание отмены
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

// --- Модель события (должна совпадать с DeviceManagementService) ---
public class DeviceTelemetryEvent
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}