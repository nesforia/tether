// windows/ChatWindow.cs
using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Tether.modules;
using Tether.states;

namespace Tether.windows;

public class ChatWindow : Window
{
    public readonly GroupChat _chat;
    private readonly Configuration _config;
    private string _input = string.Empty;
    private bool _scrollToBottom = true;
    private bool _focusInput = true;
    private bool _showParticipants = false;
    private bool _editingChatName = false;
    private string _editedChatName = string.Empty;

    private const float SidebarWidth = 155f;

    // Focus/attention tracking
    private bool _isFocused = false;
    public bool _newMessage = false;
    private bool _pulseActive;
    private float UnfocusedAlpha = 0.45f;

    private readonly Vector4 PulseRed  = new(0.85f, 0.25f, 0.30f, 1f);
    private readonly Vector4 PulseBlue = new(0.30f, 0.55f, 0.95f, 1f);

    public Action<GroupChat, string>? OnSendMessage;

    private IDisposable? _alphaStyles;
    private IDisposable? _pulseColors;
    private IDisposable? _windowColors;

    private readonly Vector4 Accent = new(0.38f, 0.62f, 1.00f, 1f);
    private readonly Vector4 AccentSoft = new(0.28f, 0.42f, 0.72f, 1f);
    private readonly Vector4 PastelBlue = new(0.17f, 0.24f, 0.39f, 1f);
    private readonly Vector4 PastelPink = new(0.18f, 0.20f, 0.28f, 1f);
    private readonly Vector4 Surface = new(0.10f, 0.11f, 0.15f, 1f);
    private readonly Vector4 Surface2 = new(0.13f, 0.14f, 0.19f, 1f);
    private readonly Vector4 Text = new(0.94f, 0.95f, 0.98f, 1f);
    private readonly Vector4 TextMuted = new(0.55f, 0.58f, 0.66f, 1f);
    private readonly Vector4 NameMine = new(0.75f, 0.65f, 1.00f, 1f);
    private readonly Vector4 NameOther = new(0.55f, 0.80f, 1.00f, 1f);

    public ChatWindow(GroupChat chat, Configuration config) : base($"{chat.Name}###GroupChat_{chat.Id}")
    {
        _chat = chat;
        _config = config;

        Size = new Vector2(470, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.None;
        RespectCloseHotkey = true;

    }

    public override void PreDraw()
    {
        var t = (float)(ImGui.GetTime() * 3.0);
        var s = (MathF.Sin(t) + 1f) / 2f;
        var pulseColor = Vector4.Lerp(PulseRed, PulseBlue, s);
        
        _pulseActive = _newMessage && _config.DISPLAY_NOTIFICATIONS_ON_NEW_MESSAGE;

        _windowColors = ImRaii.PushColor(ImGuiCol.WindowBg, Surface);
    
        if (_pulseActive)
        {
            _pulseColors = ImRaii.PushColor(ImGuiCol.TitleBg, pulseColor)
                                 .Push(ImGuiCol.TitleBgActive, pulseColor)
                                 .Push(ImGuiCol.TitleBgCollapsed, pulseColor);
        }
        
        if (!_isFocused && _alphaStyles is null)
        {
            _alphaStyles = ImRaii.PushStyle(ImGuiStyleVar.Alpha, UnfocusedAlpha);
        }
    }
    
    public override void PostDraw()
    {
        _pulseColors?.Dispose();
        _pulseColors = null;
        _alphaStyles?.Dispose();
        _alphaStyles = null;
        _windowColors?.Dispose();
        _windowColors = null;
    }

    public override void Draw()
    {
        UnfocusedAlpha = _config.OPACITY_WINDOW_CHAT_ON_UNFOCUSED;
        _isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        var compact = _config.COMPACT_CHAT_MODE;

        if (_isFocused)
        {
            using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f);
            _newMessage = false;
            _alphaStyles?.Dispose();
            _alphaStyles = null;
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 8))
                                .Push(ImGuiStyleVar.FramePadding, new Vector2(6, 4))
                                .Push(ImGuiStyleVar.ItemSpacing, compact ? new Vector2(4, 2) : new Vector2(5, 4))
                                .Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 3))
                                .Push(ImGuiStyleVar.ScrollbarSize, 10f)
                                .Push(ImGuiStyleVar.WindowRounding, 10f)
                                .Push(ImGuiStyleVar.ChildRounding, 8f)
                                .Push(ImGuiStyleVar.FrameRounding, 6f);

        using var color = ImRaii.PushColor(ImGuiCol.WindowBg, Surface)
                                .Push(ImGuiCol.ChildBg, Surface)
                                .Push(ImGuiCol.Border, new Vector4(0.20f, 0.22f, 0.29f, 1f))
                                .Push(ImGuiCol.FrameBg, Surface2)
                                .Push(ImGuiCol.FrameBgHovered, new Vector4(0.16f, 0.18f, 0.24f, 1f))
                                .Push(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.20f, 0.27f, 1f))
                                .Push(ImGuiCol.Button, AccentSoft)
                                .Push(ImGuiCol.ButtonHovered, Accent)
                                .Push(ImGuiCol.ButtonActive, new Vector4(0.32f, 0.52f, 0.88f, 1f));

        DrawChatHeader();
        ImGui.Spacing();

        if (_showParticipants)
        {
            var contentWidth = ImGui.GetContentRegionAvail().X;
            var mainWidth = MathF.Max(120f, contentWidth - SidebarWidth - ImGui.GetStyle().ItemSpacing.X);

            ImGui.BeginGroup();
            DrawMessageLog(mainWidth);
            ImGui.Spacing();
            DrawInputRow(mainWidth);
            ImGui.EndGroup();

            ImGui.SameLine();

            DrawParticipantsSidebar(SidebarWidth);
        }
        else
        {
            var fullWidth = ImGui.GetContentRegionAvail().X;
            DrawMessageLog(fullWidth);
            ImGui.Spacing();
            DrawInputRow(fullWidth);
        }

    }

    private void DrawChatHeader()
    {
        using (ImRaii.Group())
        {
            if (_editingChatName)
            {
                var inputWidth = 180f;

                ImGui.SetNextItemWidth(inputWidth);

                var enterPressed = ImGui.InputText(
                    "##EditChatName",
                    ref _editedChatName,
                    100,
                    ImGuiInputTextFlags.EnterReturnsTrue
                );

                ImGui.SameLine(0, 4);

                if (ImGui.Button("✓"))
                    SaveChatName();

                ImGui.SameLine(0, 4);

                if (ImGui.Button("X"))
                    CancelChatNameEdit();

                if (enterPressed)
                    SaveChatName();

                ImGui.SameLine(0, 6);
            }
            else
            {
                using (ImRaii.PushColor(ImGuiCol.Text, Text))
                {
                    ImGui.TextUnformatted(_chat.Name);
                }

                if (_chat.OwnerId == APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()))
                {
                    ImGui.SameLine(0, 6);

                    if (ImGui.SmallButton("Edit"))
                        BeginChatNameEdit();
                }
            }   
        }

        ImGui.SameLine();
        DrawParticipantsToggleButton();

        ImGui.Dummy(new Vector2(0, 5));
        using (ImRaii.PushColor(ImGuiCol.Separator, new Vector4(0.22f, 0.25f, 0.33f, 1f)))
        {
            ImGui.Separator();
        }
        ImGui.Dummy(new Vector2(0, 7));
    }
    
    private void BeginChatNameEdit()
    {
        _editedChatName = _chat.Name;
        _editingChatName = true;
    }

    private void SaveChatName()
    {
        if (!string.IsNullOrWhiteSpace(_editedChatName))
            _ = APIHandler.SendPOST("/group/rename", new
            {
                groupId = _chat.Id,
                newName = _editedChatName
            });
        
        _editingChatName = false;
    }

    private void CancelChatNameEdit()
    {
        _editedChatName = _chat.Name;
        _editingChatName = false;
    }

    private void DrawParticipantsToggleButton()
    {
        const float buttonSize = 30f;

        var availWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0, availWidth - buttonSize));

        using (ImRaii.PushColor(ImGuiCol.Button, _showParticipants ? Accent : null))
        using (ImRaii.PushFont(Plugin.PluginInterface.UiBuilder.FontIcon))
        {
            var button = ImGui.Button(FontAwesomeIcon.Users.ToIconString(), new Vector2(buttonSize, buttonSize));

            if (button)
                _showParticipants = !_showParticipants;
        }

        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                ImGui.TextUnformatted("Members");
            }
        }

        
    }

    private void DrawParticipantsSidebar(float width)
    {
        using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.075f, 0.08f, 0.11f, 1f)))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8f))
        using (ImRaii.Child("##Participants", new Vector2(width, 0), true))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, TextMuted))
            {
                ImGui.TextUnformatted($"{_chat.Participants.Count} MEMBER{(_chat.Participants.Count == 1 ? "" : "S")}");
            }

            ImGui.Dummy(new Vector2(0, 4));
            using (ImRaii.PushColor(ImGuiCol.Separator, new Vector4(0.22f, 0.25f, 0.33f, 1f)))
            {
                ImGui.Separator();
            }
            ImGui.Dummy(new Vector2(0, 6));

            foreach (var participant in _chat.Participants)
            {
                var isMe = APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()) == participant.id;
                var isOwner = participant.id == _chat.OwnerId;
                var ownerTextPrefix = isOwner ? "☆ " : "";

                using (ImRaii.PushColor(ImGuiCol.Text, isMe ? Accent : Text))
                {
                    ImGui.TextWrapped(isMe
                                          ? $"{ownerTextPrefix}{participant.FirstName} {participant.LastName} (you)"
                                          : $"{ownerTextPrefix}{participant.FirstName} {participant.LastName}");  
                }
                ImGui.Dummy(new Vector2(0, 4));
            }
        }
    }

    private void DrawMessageLog(float width)
    {
        const float inputAreaHeight = 62f;
        float logHeight = MathF.Max(80f, ImGui.GetContentRegionAvail().Y - inputAreaHeight);

        using (ImRaii.Child("##ChatLog", new Vector2(width, logHeight), true, ImGuiWindowFlags.HorizontalScrollbar))
        {
            var tempMsg = _chat.Messages.ToArray();
            var compact = _config.COMPACT_CHAT_MODE;

            foreach (var msg in tempMsg)
            {
                if (compact)
                    DrawMessageLineCompact(msg);
                else if (msg.Author.id != "0")
                    DrawMessageLine(msg);
                else
                    DrawSystemLine(msg);
            }

            if (_scrollToBottom || ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 5f)
                ImGui.SetScrollHereY(1.0f);

            _scrollToBottom = false;
        }
    }

    // Compact mode: one flat line per message, no bubble background, minimal
    // vertical space. System lines just render muted instead of getting their
    // own bubble treatment.
    private void DrawMessageLineCompact(ChatMessage msg)
    {
        var isSystem = msg.Author.id == "0";
        var time = $"{msg.CreatedAt:HH:mm}";

        using (ImRaii.PushColor(ImGuiCol.Text, TextMuted))
        {
            ImGui.TextUnformatted(time);
        }

        ImGui.SameLine(0, 6);

        if (isSystem)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, TextMuted))
            using (ImRaii.TextWrapPos(0))
            {
                ImGui.TextUnformatted(msg.Message);
            }
            return;
        }

        var isMine = APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()) == msg.Author.id;
        var name = $"{msg.Author.FirstName} {msg.Author.LastName}:";

        using (ImRaii.PushColor(ImGuiCol.Text, isMine ? NameMine : NameOther))
        {
            ImGui.TextUnformatted(name); 
        }

        ImGui.SameLine(0, 5);
        
        using (ImRaii.PushColor(ImGuiCol.Text, Text))
        using (ImRaii.TextWrapPos(0))
        {
            ImGui.TextUnformatted(msg.Message);
        }
    }

    private void DrawMessageLine(ChatMessage msg)
    {
        bool isMine = APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()) == msg.Author.id;

        string author = $"{msg.Author.FirstName} {msg.Author.LastName}";
        string time = $"{msg.CreatedAt:HH:mm}";

        const float paddingX = 12f;
        const float paddingY = 8f;

        float bubbleWidth = ImGui.GetContentRegionAvail().X;
        float textWidth = bubbleWidth - paddingX * 2f;

        Vector2 messageSize = ImGui.CalcTextSize(msg.Message, false, textWidth);

        float headerHeight = ImGui.GetTextLineHeight();
        float spacing = ImGui.GetStyle().ItemSpacing.Y;

        float bubbleHeight = paddingY * 2f + headerHeight + spacing + messageSize.Y + 2f;

        string id = $"##Bubble_{msg.CreatedAt.Ticks}_{msg.Author.id}";

        using (ImRaii.PushColor(ImGuiCol.ChildBg, isMine ? PastelPink : PastelBlue))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 12f).Push(ImGuiStyleVar.WindowPadding, new Vector2(paddingX, paddingY)))
        using (ImRaii.Child(id, new Vector2(bubbleWidth, bubbleHeight), true,
                            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, isMine ? Accent : Text))
            {
                ImGui.TextUnformatted(author);
            }

            ImGui.SameLine(0, 8);

            using (ImRaii.PushColor(ImGuiCol.Text, TextMuted))
            {
                ImGui.TextUnformatted(time);
            }

            using (ImRaii.PushColor(ImGuiCol.Text, Text))
            {
                ImGui.TextWrapped(msg.Message);
            }
        }

        ImGui.Dummy(new Vector2(0, 6f));
    }

    private void DrawSystemLine(ChatMessage msg)
    {
        string time = $"{msg.CreatedAt:HH:mm}";

        const float paddingX = 12f;
        const float paddingY = 8f;

        float bubbleWidth = ImGui.GetContentRegionAvail().X;
        float textWidth = MathF.Max(1f, bubbleWidth - paddingX * 2f);

        Vector2 messageSize = ImGui.CalcTextSize(msg.Message, false, textWidth);

        float timeHeight = ImGui.GetTextLineHeight();
        float spacing = ImGui.GetStyle().ItemSpacing.Y;

        float bubbleHeight = paddingY * 2f + timeHeight + spacing + messageSize.Y + 2f;
        
        string id = $"##SystemBubble_{msg.CreatedAt.Ticks}";
        
        using (ImRaii.PushColor(ImGuiCol.ChildBg, PastelPink))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 12f)
                     .Push(ImGuiStyleVar.WindowPadding, new Vector2(paddingX, paddingY)))
        using (ImRaii.Child(id, new Vector2(bubbleWidth, bubbleHeight), true,
                            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Text))
            {
                ImGui.TextUnformatted(time); 
            }

            using (ImRaii.PushColor(ImGuiCol.Text, Text))
            {
                ImGui.TextWrapped(msg.Message);  
            }
        }

        ImGui.Dummy(new Vector2(0, 6f));
    }

    private void DrawInputRow(float width)
    {
        ImGui.Dummy(new Vector2(0, 5));

        if (_focusInput)
        {
            ImGui.SetKeyboardFocusHere();
            _focusInput = false;
        }

        var buttonWidth = 68f;
        var inputWidth = width - buttonWidth - ImGui.GetStyle().ItemSpacing.X;

        ImGui.SetNextItemWidth(inputWidth);

        var sendPressed = ImGui.InputTextWithHint(
            "##ChatInput", "Write a message...", ref _input, 500, ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.SameLine();

        if (ImGui.Button("Send", new Vector2(buttonWidth, 0)))
            sendPressed = true;

        if (sendPressed && !string.IsNullOrWhiteSpace(_input))
        {
            OnSendMessage?.Invoke(_chat, _input.Trim());
            _input = string.Empty;
            _scrollToBottom = true;
            _focusInput = true;
        }
    }
}
