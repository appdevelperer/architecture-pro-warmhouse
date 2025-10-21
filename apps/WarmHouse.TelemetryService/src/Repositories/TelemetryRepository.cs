using Npgsql;
using WarmHouse.TelemetryService.Models;

namespace WarmHouse.TelemetryService.Repositories;

public class TelemetryRepository
{
    private readonly string _connectionString;

    public TelemetryRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("TelemetryDb")
            ?? throw new InvalidOperationException("Connection string 'TelemetryDb' not found.");
    }

    public async Task AddAsync(TelemetryRecord record)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand(@"
            INSERT INTO telemetry (id, device_id, is_on, location, type, timestamp)
            VALUES (@id, @device_id, @is_on, @location, @type, @timestamp)", conn);

        cmd.Parameters.AddWithValue("id", record.Id);
        cmd.Parameters.AddWithValue("device_id", record.DeviceId);
        cmd.Parameters.AddWithValue("is_on", record.IsOn);
        cmd.Parameters.AddWithValue("location", record.Location);
        cmd.Parameters.AddWithValue("type", record.Type);
        cmd.Parameters.AddWithValue("timestamp", record.Timestamp);

        await cmd.ExecuteNonQueryAsync();
    }
}