using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ECommons.ChatMethods;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using SocketIOClient;
using Tether.config;
using Tether.DTO;
using Tether.states;
using Tether.windows;

namespace Tether.modules;

public class APIHandler
{
    static string? Token = null;
    static readonly HttpClient HttpClient = new();
    private bool isFetching = false;
    private SocketIO? client;
    
    //
    private readonly WindowSystem windowSystem;
    private readonly Plugin plugin;
    
    public APIHandler(WindowSystem windowSystem, Plugin plugin)
    {
        this.windowSystem = windowSystem;
        this.plugin = plugin;
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
            }
        });

        client.OnConnected += async (Sender, e) =>
        {
            Plugin.PluginLog.Info("Connected to server");
        };

        client.OnDisconnected += (Sender, e) =>
        {
            Plugin.PluginLog.Info("Disconnected from server");
            Disconnect();
        };
        
        client.On(ESocketEvent.SEND_GROUP_REQUEST, async response =>
        {
            var shouldDecline = plugin.Configuration.DECLINE_EVERY_CHAT_REQUEST ||
                                (plugin.Configuration.BLOCK_INVITES_WHILE_IN_DUTY && Plugin.DutyState.IsDutyStarted);
            
            if (!shouldDecline)
            {
                var payload = response.GetValue<PlayerState>(0);
            
                Plugin.PluginLog.Info($"Received group request from {payload.FirstName} {payload.LastName}");
            
                var window = new RequestWindow($"{payload.FirstName} {payload.LastName}", payload.id);
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
            
            var window = new RequestWindow($"{payload.firstName} {payload.lastName}", null, payload.id);
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
        if (Token is not null) return;
        if (isFetching) return;
        isFetching = true;

        var payload = new
        {
            id = HashString(Plugin.PlayerState.ContentId.ToString()),
            firstName = Plugin.PlayerState.CharacterName.Split(" ")[0],
            lastName = Plugin.PlayerState.CharacterName.Split(" ")[1]
        };

        var response = await HttpClient.PostAsJsonAsync(Secrets.URL + "/auth", payload);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<RequestTokenPayload>();
            Token = result?.Token;
            isFetching = false;
            ConnectToSocket();
        }
        else
        {
            Plugin.PluginLog.Error($"Cannot connect to server, retrying...");
            await Task.Delay(10000);
            isFetching = false;
        }
    }

    public async Task Disconnect()
    {
        if (client is null) return;

        await client.DisconnectAsync();
        client.Dispose();
        client = null;
        Token = null;
        isFetching = false;
        
        // Clear chats
        if (plugin.ChatModule.Chats.ToList().Count > 0)
        {
            plugin.ChatModule.Chats.ToList().ForEach(chat =>
            {
                plugin.ChatModule.RemoveGroup(chat.Id);
                _ = SendPOST("/group/leave", new { id = chat.Id });
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
    
    // POST
    public static async Task<HttpResponseMessage>? SendPOST(string path, object payload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Secrets.URL + path);
            request.Headers.Add("x-auth-token", Token);
            request.Content = JsonContent.Create(payload);

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Plugin.PluginLog.Error(
                    $"Cannot send a request. Status: {(int)response.StatusCode} {response.ReasonPhrase}"
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Request to {path} failed");
        }

        return null;
    }
}
