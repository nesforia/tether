using System;
using System.Text.Json.Serialization;

namespace Tether.states;

public class PlayerState
{
    private string _id;
    private string _firstName;
    private string _lastName;
    private string _world;

    public PlayerState(string id, string firstName, string lastName, string world)
    {
        _id = id;
        _firstName = firstName;
        _lastName = lastName;
        _world = world;
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

    [JsonPropertyName("world")]
    public string World
    {
        get => _world;
        set => _world = value ?? throw new ArgumentNullException(nameof(value));
    }
}
