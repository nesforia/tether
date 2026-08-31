using System;

namespace Tether.states;

public class ChatMessage
{
    private PlayerState _author;
    private string _message;

    public ChatMessage(PlayerState author, string message, DateTime createdAt)
    {
        _author = author;
        _message = message;
        this.CreatedAt = createdAt;
    }

    public PlayerState Author
    {
        get => _author;
        set => _author = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Message
    {
        get => _message;
        set => _message = value ?? throw new ArgumentNullException(nameof(value));
    }

    public DateTime CreatedAt { get; set; }
}
