namespace WarmHouse.DeviceManagementService.Models;

public record Device
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "heater"; // heater, light, gate, camera...
    public string Location { get; set; } = string.Empty;
    public bool IsOn { get; set; } = false;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}