var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Словари для сопоставления location ↔ sensorId
var locationToSensorId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["Living Room"] = "1",
    ["Bedroom"] = "2",
    ["Kitchen"] = "3"
};

var sensorIdToLocation = new Dictionary<string, string>
{
    ["1"] = "Living Room",
    ["2"] = "Bedroom",
    ["3"] = "Kitchen"
};

// /temperature
app.MapGet("/temperature", (string? location, string? sensorId) =>
{
    // 1. Если location не задан — определяем по sensorId
    if (string.IsNullOrWhiteSpace(location))
    {
        if (!string.IsNullOrWhiteSpace(sensorId) && sensorIdToLocation.TryGetValue(sensorId, out var loc))
        {
            location = loc;
        }
        else
        {
            location = "Unknown";
        }
    }

    // 2. Если sensorId не задан — определяем по location
    if (string.IsNullOrWhiteSpace(sensorId))
    {
        if (locationToSensorId.TryGetValue(location, out var id))
        {
            sensorId = id;
        }
        else
        {
            sensorId = "0";
        }
    }

    // 3. Генерируем случайную температуру
    var temperatureC = Random.Shared.Next(-20, 55);

    // 4. Возвращаем результат
    return Results.Ok(new
    {
        sensorId = sensorId,
        location = location,
        Value = temperatureC,           // ← вместо temperatureC
        unit = "°C",                    // ← явно указываем
        status = "active",              // ← можно сделать динамическим
        timestamp = DateTime.UtcNow,    // ← важно: в UTC и в формате ISO 8601
        Description = "ответc от сервиса"
    });
    // return Results.Ok(5);
})
.WithName("GetTemperature")
.WithOpenApi(); 


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
