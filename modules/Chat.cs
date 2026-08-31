using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Network.Structures.InfoProxy;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices.Legacy;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using Tether.states;
using Tether.windows;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace Tether.modules;

public class Chat
{
    private readonly Plugin plugin;
    
    private List<GroupChat> _chats = new();
    private List<ChatWindow> _windows = new();
    public PlayerState SystemAuthor = new PlayerState("0", "Unknown", "Unknown", "Unknown");
    private PlayerState? localAuthor;
    
    private PlayerState LocalAuthor
    {
        get
        {
            if (localAuthor is null)
            {
                var fullName = Plugin.PlayerState.CharacterName ?? string.Empty;
                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var firstName = parts.Length > 0 ? parts[0] : "Unknown";
                var lastName = parts.Length > 1 ? parts[1] : string.Empty;

                localAuthor = new PlayerState(
                    Plugin.PlayerState.ContentId.ToString(),
                    firstName,
                    lastName,
                    Plugin.PlayerState.HomeWorld.Value.Name.ToString());
            }
            return localAuthor;
        }
    }
    
    // Provided
    private WindowSystem windowSystem;

    public Chat(WindowSystem windowSystem, Plugin plugin)
    {
        this.windowSystem = windowSystem;
        this.plugin = plugin;
    }
    
    // Functions
    public void InviteToChat(string? id, MenuTargetDefault? target = null)
    {
        if (target is null) return;
        if (id is null)
        {
            CreateRequestChat(target.TargetContentId.ToString());
        }
        else
        {
            SendInvite(id, target.TargetContentId.ToString());
        }
    }

    public void CreateRequestChat(string contentId)
    {
        APIHandler.SendPOST("/invite", new
        {
            id = contentId
        });
    }
    
    public void CreateChat(string chatId, PlayerState[] participants, string ownerId)
    {
        var groupchat = new GroupChat(chatId, "GroupChat", ownerId);
        groupchat.Participants = participants.ToList();
        _chats.Add(groupchat);

        var window = new ChatWindow(groupchat, plugin.Configuration);
        window.OnSendMessage = (chat, message) => SendMessage(chat.Id, message);
        window.IsOpen = true;
        
        _windows.Add(window);
        windowSystem.AddWindow(window);
    }

    public void SendMessage(string id, string message)
    {
        GroupChat? chat = FindChat(id);
        if (chat is null) return;

        APIHandler.SendPOST("/group/sendMessage", new
        {
            id = chat.Id,
            message
        });
        
        chat.Messages.Add(new ChatMessage(LocalAuthor, message, DateTime.Now));
    }

    public unsafe void AddMessage(string id, string message, PlayerState? author)
    {
        GroupChat? chat = FindChat(id);
        if (chat is null) return;
        if (author is null) return;
        if (author.id == LocalAuthor.id) return;
        
        chat.Messages.Add(new ChatMessage(author, message, DateTime.Now));
        var chatWindow = FindWindow(chat.Id);
        if (chatWindow is null) return;

        if (plugin.Configuration.AUTO_OPEN_WINDOW_ON_NEW_CHAT)
        {
            chatWindow.IsOpen = true;
        }
        
        chatWindow._newMessage = true;
        if (plugin.Configuration.SOUND_NOTIFICATION_ON_NEW_MESSAGE && !chatWindow.IsFocused)
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                UIGlobals.PlayChatSoundEffect(14);
            });
        }
    }

    public void SendInvite(string id, string contentId)
    {
        GroupChat? chat = FindChat(id);
        if (chat is null) return;
        
        APIHandler.SendPOST("/group/invite", new
        {
            id = chat.Id,
            to = contentId
        });
    }

    public void RemoveGroup(string chatId)
    {
        GroupChat? chat = FindChat(chatId);
        if (chat is null) return;
    
        var window = FindWindow(chat.Id);
        if (window is not null)
        {
            window.IsOpen = false;
            _windows.Remove(window);
            windowSystem.RemoveWindow(window);
        }
    
        _chats.Remove(chat);
    }

    public void UserLeaveChat(string groupId, PlayerState player)
    {
        GroupChat? chat = FindChat(groupId);
        if (chat is null) return;
        var user = chat.Participants.Find(s => s.id == player.id);
        chat.Participants.Remove(user);
        
        AddMessage(groupId, $"User {user.FirstName} {user.LastName} left the channel.", SystemAuthor);
    }
    
    public void UserEnterChat(string groupId, PlayerState player)
    {
        GroupChat? chat = FindChat(groupId);
        if (chat is null) return;
        chat.Participants.Add(player);
        
        AddMessage(groupId, $"User {player.FirstName} {player.LastName} enter the channel.", SystemAuthor);
    }

    public void RenameChat(string groupId, string name)
    {
        GroupChat? chat = FindChat(groupId);
        if (chat is null) return;

        chat.Name = name;
        
        AddMessage(groupId, $"Group name was changed to {name}", SystemAuthor);
    }

    public void UpdateOwner(string groupId, string ownerId)
    {
        GroupChat? chat = FindChat(groupId);
        if (chat is null) return;

        chat.OwnerId = ownerId;
    }

    // Utils
    public void DrawContextMenu(IMenuOpenedArgs args)
    {
        if (args.Target is not MenuTargetDefault target) return;
        if (target.TargetObject is { } obj && obj.ObjectKind != ObjectKind.Pc) return;

        args.AddMenuItem(new MenuItem
        {
            Name = "Groups",
            Prefix = SeIconChar.BoxedLetterG,
            IsSubmenu = true,
            OnClicked = clickArgs =>
            {
                var submenuItems = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Name = "Add New Group",
                        Prefix = SeIconChar.BoxedPlus,
                        OnClicked = _ => InviteToChat(null, target),
                    },
                };
                

                if (_chats.Count > 0)
                {
                    submenuItems.AddRange(_chats.Select(chat => new MenuItem
                    {
                        Name = chat.Name,
                        Prefix = SeIconChar.BoxedLetterC,
                        OnClicked = _ => InviteToChat(chat.Id, target),
                    }));
                }

                clickArgs.OpenSubmenu(submenuItems);
            },
        });
    }

    public ChatWindow? FindWindow(string id)
    {
        return _windows.Find(s => s._chat.Id == id);
    }

    public GroupChat? FindChat(string id)
    {
        return _chats.Find(s => s.Id == id);
    }
    
    public void Dispose()
    {
        _chats.Clear();
        _windows.Clear();
    }
    
    // Getters
    public List<GroupChat> Chats
    {
        get => _chats;
        set => _chats = value ?? throw new ArgumentNullException(nameof(value));
    }
}
