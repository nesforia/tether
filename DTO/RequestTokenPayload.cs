using System.Text.Json.Serialization;

namespace Tether.DTO;

public class RequestTokenPayload
{
    [JsonPropertyName("token")]
    public required string Token { get; set; }
    [JsonPropertyName("usercode")]
    public required string UserCode { get; set; }
}
