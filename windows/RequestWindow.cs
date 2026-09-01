// windows/RequestWindow.cs
using System;
using System.Net.Http.Json;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Tether.DTO;
using Tether.modules;
using Tether.states;

namespace Tether.windows;

public class RequestWindow : Window
{
    private const float DurationSeconds = 15f;

    private readonly string _groupId;
    private readonly string _userInviteName;
    private readonly string _userInviteId;
    private float _remaining = DurationSeconds;

    public Action<RequestWindow>? OnClosed;
    public Action<string, PlayerState[], string>? OnAcceptedGroupInvite;

    private static readonly Vector4 Accent = new(0.38f, 0.62f, 1.00f, 1f);
    private static readonly Vector4 Surface = new(0.10f, 0.11f, 0.15f, 1f);
    private static readonly Vector4 Surface2 = new(0.13f, 0.14f, 0.19f, 1f);
    private static readonly Vector4 Text = new(0.94f, 0.95f, 0.98f, 1f);
    private static readonly Vector4 TextMuted = new(0.55f, 0.58f, 0.66f, 1f);
    private static readonly Vector4 Green = new(0.35f, 0.86f, 0.62f, 1f);
    private static readonly Vector4 Red = new(0.95f, 0.34f, 0.40f, 1f);

    public RequestWindow(string userInviteName, string userInviteId, string? groupId = null)
        : base($"New Chat Invite###Invite_{userInviteId}_{Guid.NewGuid():N}")
    {
        _userInviteName = userInviteName;
        _userInviteId = userInviteId;
        _groupId = groupId;

        Size = new Vector2(360, 215);
        SizeCondition = ImGuiCond.Always;

        Flags = ImGuiWindowFlags.NoResize
              | ImGuiWindowFlags.NoCollapse
              | ImGuiWindowFlags.NoScrollbar;
    }

    public override void Update()
    {
        _remaining -= ImGui.GetIO().DeltaTime;

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            Decline();
        }
    }

    public override void Draw()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 8))
                                .Push(ImGuiStyleVar.FramePadding, new Vector2(6, 4))
                                .Push(ImGuiStyleVar.ItemSpacing, new Vector2(5, 4))
                                .Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 3))
                                .Push(ImGuiStyleVar.WindowRounding, 10f)
                                .Push(ImGuiStyleVar.FrameRounding, 6f)
                                .Push(ImGuiStyleVar.ChildRounding, 8f);

        using var color = ImRaii.PushColor(ImGuiCol.WindowBg, Surface)
                                .Push(ImGuiCol.Border, new Vector4(0.20f, 0.22f, 0.29f, 1f))
                                .Push(ImGuiCol.FrameBg, Surface2);

        using (ImRaii.PushColor(ImGuiCol.Text, Accent))
        {
            ImGui.TextUnformatted("NEW GROUP CHAT");
        }
        
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 5f);

        using (ImRaii.PushColor(ImGuiCol.Text, TextMuted))
        {
            ImGui.TextUnformatted($"{Math.Ceiling(_remaining):0}s");
        }

        ImGui.Dummy(new Vector2(0, 4));

        DrawTimerBar();

        ImGui.Dummy(new Vector2(0, 4));

        using (ImRaii.PushColor(ImGuiCol.Text, Text))
        {
            ImGui.TextUnformatted($"{_userInviteName} wants to chat with you.");
        }

        using (ImRaii.PushColor(ImGuiCol.Text, Text))
        {
            ImGui.TextUnformatted("The invite will disappear when the timer runs out.");
        }

        ImGui.Dummy(new Vector2(0, 2));

        var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;
        
        using (ImRaii.PushColor(ImGuiCol.Button, Green)
                     .Push(ImGuiCol.ButtonHovered, new Vector4(0.43f, 0.94f, 0.70f, 1f))
                     .Push(ImGuiCol.ButtonActive, new Vector4(0.28f, 0.73f, 0.52f, 1f)))
        {
            if (ImGui.Button("Accept", new Vector2(buttonWidth, 34)))
                Accept();
        }

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.Button, Surface2)
                     .Push(ImGuiCol.ButtonHovered, new Vector4(0.19f, 0.21f, 0.27f, 1f))
                     .Push(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.17f, 0.22f, 1f))
                     .Push(ImGuiCol.Text, TextMuted))
        {
            if (ImGui.Button("Decline", new Vector2(buttonWidth, 34)))
                Decline();
        }
    }


    private void DrawTimerBar()
    {
        var fraction = Math.Clamp(_remaining / DurationSeconds, 0f, 1f);
        var barColor = fraction > 0.3f ? Green : Red;
        
        using (ImRaii.PushColor(ImGuiCol.PlotHistogram, barColor)
                     .Push(ImGuiCol.FrameBg, Surface2))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 6f))
        {
            ImGui.ProgressBar(fraction, new Vector2(-1, 7), string.Empty);
        }
    }

    private async void Accept()
    {
        if (_groupId is null)
        {
            _ = APIHandler.SendPOST("/group/create", new { from = _userInviteId });
        }
        else
        {
            var response = await APIHandler.SendPOST("/group/acceptInvite", new { id = _groupId });
            var groupParticipants = await response.Content.ReadFromJsonAsync<AcceptGroupRequestPayload>();
            if (groupParticipants is null) return;
            
            OnAcceptedGroupInvite?.Invoke(_groupId, groupParticipants.participants, groupParticipants.ownerId);
        }

        Close();
    }

    private void Decline()
    {
        // No decline notification is wired yet; closing the invite remains the current behavior.
        Close();
    }

    private void Close()
    {
        IsOpen = false;
        OnClosed?.Invoke(this);
    }
}
