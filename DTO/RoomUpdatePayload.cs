using System.Text.Json.Serialization;
using Tether.states;

namespace Tether.DTO;

public class RoomUpdatePayload
{
    [JsonPropertyName("group")]
    public required string group { get; set; }
    
    [JsonPropertyName("user")]
    public PlayerState? user { get; set; } = null;
    
    [JsonPropertyName("payload")]
    public string? payload { get; set; } = string.Empty;
    
    [JsonPropertyName("action")]
    public required string action { get; set; }
}
