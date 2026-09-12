using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using SocketIOClient;
using Tether.config;
using Tether.DTO;
using Tether.enums;
using Tether.states;
using Tether.windows;

namespace Tether.modules;

public class APIHandler
{
    static string? Token = null;
    static readonly HttpClient HttpClient = new();
    public static bool isFetching = false;
    private SocketIO? client;
    private static APIHandler? _instance;
    
    private readonly WindowSystem windowSystem;
    private readonly Plugin plugin;
    
    public APIHandler(WindowSystem windowSystem, Plugin plugin)
    {
        this.windowSystem = windowSystem;
        this.plugin = plugin;
        _instance = this;
    }

    /*
     * Main function to connect to Socket IO of backend service
     * Getting events and turning them to functions
     */
    private void ConnectToSocket()
    {
        client = new SocketIO(new Uri(Secrets.URL), new SocketIOOptions
        {
            Auth = new Dictionary<string, string>
            {
                { "token", Token }
            },
            Reconnection = true,
            ReconnectionDelayMax = 5000
        });

        client.OnConnected += async (Sender, e) =>
        {
            Plugin.PluginLog.Info("Connected to server");
        };

        client.OnDisconnected += async (Sender, e) =>
        {
            // Clearing chats
            await Disconnect();
            
            // Try to connect to server again after losing socket connection with server
            if (e == EErrorMessageReturn.TRANSPORT_ERROR)
            {
                Plugin.PluginLog.Info($"Server is not responding.");
                return;
            }
            
            Plugin.PluginLog.Info($"Disconnected from server");
        };

        client.OnReconnectAttempt += async (Sender, e) =>
        {
            Plugin.PluginLog.Info($"Reconnecting to server...");
            if (!isFetching)
            {
                Token = null;
                await GenerateUserToken();
                if (client?.Options?.Auth is Dictionary<string, string> auth && Token is not null)
                    auth["token"] = Token;
            }
        };
        
        client.On(ESocketEvent.SEND_GROUP_REQUEST, async response =>
        {
            var shouldDecline = plugin.Configuration.DECLINE_EVERY_CHAT_REQUEST ||
                                (plugin.Configuration.BLOCK_INVITES_WHILE_IN_DUTY && Plugin.DutyState.IsDutyStarted);
            
            if (!shouldDecline)
            {
                var payload = response.GetValue<PlayerState>(0);
            
                Plugin.PluginLog.Info($"Received group request from {payload.FirstName} {payload.LastName}");
            
                var window = new RequestWindow(plugin.Configuration, $"{payload.FirstName} {payload.LastName}", payload.id);
                window.IsOpen = true;
                window.OnClosed += w => windowSystem.RemoveWindow(w);
            
                windowSystem.AddWindow(window);
            }
        });
        
        client.On(ESocketEvent.ACCEPT_GROUP_REQUEST, async response =>
        {
            var payload = response.GetValue<AcceptGroupRequestPayload>(0);
            if (payload is null) return;
            
            plugin.ChatModule.CreateChat(payload.id, payload.participants, payload.ownerId);
        });
        
        
        client.On(ESocketEvent.SEND_GROUP_MESSAGE, async response =>
        {
            var payload = response.GetValue<SendMessageRequestPayload>(0);
            
            plugin.ChatModule.AddMessage(payload.Id, payload.Message, payload.Author);
        });
        
        client.On(ESocketEvent.INVITE_TO_GROUP, async response =>
        {
            var payload =  response.GetValue<SendInviteGroupRequestPayload>(0);
            
            var window = new RequestWindow(plugin.Configuration, $"{payload.firstName} {payload.lastName}", null, payload.id);
            window.IsOpen = true;
            window.OnClosed += w => windowSystem.RemoveWindow(w);
            window.OnAcceptedGroupInvite += (groupid, participants, ownerId) =>
            {
                plugin.ChatModule.CreateChat(groupid, participants, ownerId);
            };
            
            windowSystem.AddWindow(window);
        });

        client.On(ESocketEvent.UPDATE_ROOM, async response =>
        {
            var payload = response.GetValue<RoomUpdatePayload>(0);
            
            if (ERoomUpdateAction.PARTICIPANT_JOIN == payload.action)
            {
                plugin.ChatModule.UserEnterChat(payload.group, payload.user);
            }

            if (ERoomUpdateAction.PARTICIPANT_LEAVE == payload.action)
            {
                plugin.ChatModule.UserLeaveChat(payload.group, payload.user);
                if (!string.IsNullOrEmpty(payload.payload))
                {
                    plugin.ChatModule.UpdateOwner(payload.group, payload.payload);
                }
            }
            
            if (ERoomUpdateAction.GROUP_NAME_CHANGE == payload.action)
            {
                plugin.ChatModule.RenameChat(payload.group, payload.payload);
            }
        });
        
        client.ConnectAsync();
    }

    public async Task GenerateUserToken()
    {
        try
        {
            if (Token is not null) return;
            if (isFetching) return;
            isFetching = true;

            var payload = new
            {
                id = HashString(Plugin.PlayerState.ContentId.ToString()),
                firstName = Plugin.PlayerState.CharacterName.Split(" ")[0],
                lastName = Plugin.PlayerState.CharacterName.Split(" ")[1]
            };

            Plugin.PluginLog.Info($"Fetching user token...");
            var response = await HttpClient.PostAsJsonAsync(Secrets.URL + "/auth", payload);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RequestTokenPayload>();
                Token = result?.Token;

                if (client is null)
                {
                    ConnectToSocket();
                    await FetchRooms();
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Warning("Failed to generate user token, server may be unreachable.");
            await Task.Delay(10000);
        } finally { 
            Plugin.PluginLog.Information($"Token for user generated: {Token}");
            isFetching = false;
        }
    }

    public async Task Disconnect()
    {
        if (client is null) return;

        await client?.DisconnectAsync();
        client?.Dispose();
        client = null;
        Token = null;
        isFetching = false;
        
        // Clear chats
        if (plugin.ChatModule.Chats.ToList().Count > 0)
        {
            plugin.ChatModule.Chats.ToList().ForEach(chat =>
            {
                plugin.ChatModule.RemoveGroup(chat.Id);
                _ = SendApiRequest("/group/leave", new { id = chat.Id });
            });
        }
    }

    public static string HashString(string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(Secrets.HASH_KEY);
        var payloadBytes =  Encoding.UTF8.GetBytes(payload);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return Convert.ToHexString(hash);
    }

    public async Task FetchRooms()
    {
        var req = await SendApiRequest("/rooms", new() {}, HttpMethod.Get);
        if (req is null) return;
        
        var rooms = await req.Content.ReadFromJsonAsync<GroupChat[]>();
        
        foreach (var groupChat in rooms)
        {
            if (plugin.ChatModule.Chats.Find(s => s.Id == groupChat.Id) is not null) continue;
            plugin.ChatModule.CreateChat(groupChat.Id, groupChat.Participants.ToArray(), groupChat.OwnerId);
            
        }
    }
    
    // STATIC UTILS
    public static async Task<HttpResponseMessage?> SendApiRequest(string path, object payload, HttpMethod? method = null)
    {
        const int maxRetries = 5;

        try
        {
            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (attempt > 0) await Task.Delay(1000);
                
                using var request = new HttpRequestMessage(method ?? HttpMethod.Post, Secrets.URL + path);
                request.Headers.Add("x-auth-token", Token);
                request.Content = JsonContent.Create(payload);

                var response = await HttpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                var error = await response.Content.ReadFromJsonAsync<ErrorReturn>();
                if (error?.message == EErrorMessageReturn.TOKEN_EXPIRED)
                {
                    Token = null;
                    Plugin.PluginLog.Warning($"Trying to refresh token... {attempt}/{maxRetries}");

                    await (_instance?.GenerateUserToken() ?? Task.CompletedTask);
                }
                
            }

            Plugin.PluginLog.Warning($"Request failed after {maxRetries} attempts.");
            return null;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Warning(ex, $"Request to {path} failed");
        }

        return null;
    }
}
