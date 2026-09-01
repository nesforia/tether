using System;
using System.Text.Json.Serialization;

namespace Tether.states;

public class PlayerState
{
    private string _id;
    private string _firstName;
    private string _lastName;

    public PlayerState(string id, string firstName, string lastName)
    {
        _id = id;
        _firstName = firstName;
        _lastName = lastName;
    }

    [JsonPropertyName("id")]
    public string id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }

    [JsonPropertyName("firstName")]
    public string FirstName
    {
        get => _firstName;
        set => _firstName = value ?? throw new ArgumentNullException(nameof(value));
    }

    [JsonPropertyName("lastName")]
    public string LastName
    {
        get => _lastName;
        set => _lastName = value ?? throw new ArgumentNullException(nameof(value));
    }
}
