using WarmHouse.DeviceManagementService.Models;
using Npgsql;

namespace  WarmHouse.DeviceManagementService.Repositories;

public class DeviceRepository
{
    private readonly string _connectionString;

    public DeviceRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DeviceDb")
            ?? throw new InvalidOperationException("Connection string 'DeviceDb' not found.");
    }

    public async Task<Device?> GetByIdAsync(string id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT id, name, type, location, is_on, last_seen, created_at FROM devices WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new Device
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Type = reader.GetString(2),
            Location = reader.GetString(3),
            IsOn = reader.GetBoolean(4),
            LastSeen = reader.GetDateTime(5),
            CreatedAt = reader.GetDateTime(6)
        };
    }

    public async Task<IEnumerable<Device>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT id, name, type, location, is_on, last_seen, created_at FROM devices", conn);
        var devices = new List<Device>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            devices.Add(new Device
            {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Type = reader.GetString(2),
            Location = reader.GetString(3),
            IsOn = reader.GetBoolean(4),
            LastSeen = reader.GetDateTime(5),
            CreatedAt = reader.GetDateTime(6)
            });
        }
        return devices;
    }

    public async Task AddAsync(Device device)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO devices (id, name, type, location, is_on, last_seen, created_at)
            VALUES (@id, @name, @type, @location, @is_on, @last_seen, @created_at)", conn);
        cmd.Parameters.AddWithValue("id", device.Id);
        cmd.Parameters.AddWithValue("name", device.Name);
        cmd.Parameters.AddWithValue("type", device.Type);
        cmd.Parameters.AddWithValue("location", device.Location);
        cmd.Parameters.AddWithValue("is_on", device.IsOn);
        cmd.Parameters.AddWithValue("last_seen", device.LastSeen);
        cmd.Parameters.AddWithValue("created_at", device.CreatedAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateStateAsync(string id, bool isOn)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "UPDATE devices SET is_on = @is_on, last_seen = @last_seen WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("is_on", isOn);
        cmd.Parameters.AddWithValue("last_seen", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }
}