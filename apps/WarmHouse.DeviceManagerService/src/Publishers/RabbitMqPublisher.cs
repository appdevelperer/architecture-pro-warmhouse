// --- RabbitMQ Publisher ---
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private static readonly string _exchangeName = "telemetry.events";

    // Приватный конструктор — создаётся только через статический асинхронный метод
    private RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
    }

  public static async Task<RabbitMqPublisher> CreateAsync(
        string hostName = "rabbitmq",
        int port = 5672,
        string userName = "admin",
        string password = "password123")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync();

        // Объявляем exchange один раз
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange: _exchangeName, ExchangeType.Fanout, durable: true);

        return new RabbitMqPublisher(connection);
    }
    
   public async Task PublishAsync<T>(string exchange, T message)
    {
        await using var channel = await _connection.CreateChannelAsync();
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: "",
            body: body,
            mandatory: false);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}