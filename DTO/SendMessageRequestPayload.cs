using System.Text.Json.Serialization;
using Tether.states;

namespace Tether.DTO;

public class SendMessageRequestPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("author")]
    public PlayerState Author { get; set; }
}
