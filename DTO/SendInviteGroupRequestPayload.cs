using System.Text.Json.Serialization;
using Tether.states;

namespace Tether.DTO;

public class SendInviteGroupRequestPayload
{
    [JsonPropertyName("id")]
    public string id { get; set; }
    
    [JsonPropertyName("firstName")]
    public string firstName  { get; set; }
    
    [JsonPropertyName("lastName")]
    public string lastName { get; set; }
}
