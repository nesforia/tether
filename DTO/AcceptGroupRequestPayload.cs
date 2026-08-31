using System.Text.Json.Serialization;
using Tether.states;

namespace Tether.DTO;

public class AcceptGroupRequestPayload
{
    [JsonPropertyName("id")]
    public string id { get; set; }
    
    [JsonPropertyName("name")]
    public string name { get; set; }
    
    [JsonPropertyName("participants")]
    public required PlayerState[] participants { get; set; }
    
    [JsonPropertyName("ownerId")]
    public required string ownerId { get; set; }
}
