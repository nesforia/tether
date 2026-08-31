using System;
using System.Collections.Generic;

namespace Tether.states;

public class GroupChat
{
    private string _id;
    private string _name;
    private List<ChatMessage> _messages = new List<ChatMessage>();
    private List<PlayerState> _participants = new List<PlayerState>();
    private string _ownerId;

    public GroupChat(string id, string name, string ownerId)
    {
        _id = id;
        _name = name;
        _ownerId = ownerId;
    }

    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name
    {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }

    public List<ChatMessage> Messages
    {
        get => _messages;
        set => _messages = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public List<PlayerState> Participants
    {
        get => _participants;
        set => _participants = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string OwnerId
    {
        get => _ownerId;
        set => _ownerId = value ?? throw new ArgumentNullException(nameof(value));
    }
}
