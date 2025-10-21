using System.Text.Json;
using Microsoft.OpenApi.Models;
using WarmHouse.DeviceManagementService.Models;
using WarmHouse.DeviceManagementService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddSingleton<DeviceRepository>();
builder.Services.AddHttpClient();

var rabbitPublisher = await RabbitMqPublisher.CreateAsync(
    hostName: "rabbitmq",
    port: 5672,
    userName: "admin",
    password: "password123"
);
// RabbitMQ Publisher как Singleton
builder.Services.AddSingleton(rabbitPublisher);

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

// --- Внешний опрос ---
async Task<DeviceStatus> GetExternalStatus(string location, HttpClient httpClient)
{
    var url = $"http://sensor-service:8081/temperature?location={Uri.EscapeDataString(location)}";
    using var response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
    var root = doc.RootElement;
    return new DeviceStatus(
        Location: root.GetProperty("location").GetString() ?? location,
        IsActive: root.GetProperty("status").GetString() == "active",
        Value: root.GetProperty("value").GetInt32(),
        Timestamp: DateTime.UtcNow
    );
}



// --- Endpoints ---
app.MapPost("/devices", async (Device device, DeviceRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(device.Location))
        return Results.BadRequest("Location is required");
    await repo.AddAsync(device);
    return Results.Created($"/devices/{device.Id}", device);
});

app.MapGet("/devices", async (DeviceRepository repo) =>
    Results.Ok(await repo.GetAllAsync()));

app.MapGet("/devices/{deviceId}/status", async (string deviceId, DeviceRepository repo, HttpClient httpClient) =>
{
    var device = await repo.GetByIdAsync(deviceId);
    if (device == null) return Results.NotFound();

    try
    {
        var external = await GetExternalStatus(device.Location, httpClient);
        return Results.Ok(new
        {
            device.Id,
            device.Name,
            device.Type,
            device.Location,
            IsOn = device.IsOn,
            Temperature = external.Value,
            Status = external.IsActive ? "active" : "inactive",
            LastUpdated = external.Timestamp
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to fetch sensor data: {ex.Message}", statusCode: 502);
    }
});

app.MapPost("/devices/{deviceId}/command", async (string deviceId, CommandRequest cmd, DeviceRepository repo, RabbitMqPublisher publisher) =>
{
    var device = await repo.GetByIdAsync(deviceId);
    if (device == null) return Results.NotFound();
    if (cmd.Command != "on" && cmd.Command != "off")
        return Results.BadRequest("Command must be 'on' or 'off'");

    var isOn = cmd.Command == "on";
    await repo.UpdateStateAsync(deviceId, isOn);

        // 📢 Публикуем событие!
    var telemetryEvent = new DeviceTelemetryEvent
    {
        DeviceId = deviceId,
        IsOn = isOn,
        Timestamp = DateTime.UtcNow,
        Location = device.Location,
        Type = device.Type
    };

    await publisher.PublishAsync("telemetry.events", telemetryEvent);

    return Results.Ok(new { message = "Command executed", deviceId, isOn });
});



app.Run();

record CommandRequest(string Command);

record DeviceStatus(string Location, bool IsActive, int Value, DateTime Timestamp);