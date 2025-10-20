namespace WarmHouse.TelemetryService.Models;

public class TelemetryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}