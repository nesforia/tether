// windows/ChatWindow.cs
using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
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

    private static readonly Vector4 PulseRed  = new(0.85f, 0.25f, 0.30f, 1f);
    private static readonly Vector4 PulseBlue = new(0.30f, 0.55f, 0.95f, 1f);

    public Action<GroupChat, string>? OnSendMessage;

    private static readonly Vector4 Accent = new(0.38f, 0.62f, 1.00f, 1f);
    private static readonly Vector4 AccentSoft = new(0.28f, 0.42f, 0.72f, 1f);
    private static readonly Vector4 PastelBlue = new(0.17f, 0.24f, 0.39f, 1f);
    private static readonly Vector4 PastelPink = new(0.18f, 0.20f, 0.28f, 1f);
    private static readonly Vector4 Surface = new(0.10f, 0.11f, 0.15f, 1f);
    private static readonly Vector4 Surface2 = new(0.13f, 0.14f, 0.19f, 1f);
    private static readonly Vector4 Text = new(0.94f, 0.95f, 0.98f, 1f);
    private static readonly Vector4 TextMuted = new(0.55f, 0.58f, 0.66f, 1f);
    private static readonly Vector4 NameMine = new(0.75f, 0.65f, 1.00f, 1f);
    private static readonly Vector4 NameOther = new(0.55f, 0.80f, 1.00f, 1f);

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
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _isFocused ? 1f : UnfocusedAlpha);

        _pulseActive = _newMessage && _config.DISPLAY_NOTIFICATIONS_ON_NEW_MESSAGE;

        if (_pulseActive)
        {
            var t = (float)(ImGui.GetTime() * 3.0);
            var s = (MathF.Sin(t) + 1f) / 2f;
            var pulseColor = Vector4.Lerp(PulseRed, PulseBlue, s);

            ImGui.PushStyleColor(ImGuiCol.TitleBg, pulseColor);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, pulseColor);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, pulseColor);
        }
    }

    public override void PostDraw()
    {
        if (_pulseActive)
            ImGui.PopStyleColor(3);

        ImGui.PopStyleVar();
    }

    public override void Draw()
    {
        _isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        UnfocusedAlpha = _config.OPACITY_WINDOW_CHAT_ON_UNFOCUSED;

        if (_isFocused)
            _newMessage = false;

        ApplyWindowStyle();

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

        EndWindowStyle();
    }

    private void ApplyWindowStyle()
    {
        var compact = _config.COMPACT_CHAT_MODE;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 8));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, compact ? new Vector2(4, 2) : new Vector2(5, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6f);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Surface);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.075f, 0.08f, 0.11f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.20f, 0.22f, 0.29f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Surface2);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.16f, 0.18f, 0.24f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.20f, 0.27f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Button, AccentSoft);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Accent);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.32f, 0.52f, 0.88f, 1f));
    }

    private static void EndWindowStyle()
    {
        ImGui.PopStyleColor(9);
        ImGui.PopStyleVar(8);
    }

    private void DrawChatHeader()
    {
        ImGui.BeginGroup();

        ImGui.PushStyleColor(ImGuiCol.Text, Accent);
        ImGui.TextUnformatted("●");
        ImGui.PopStyleColor();

        ImGui.SameLine(0, 8);

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
            ImGui.PushStyleColor(ImGuiCol.Text, Text);
            ImGui.TextUnformatted(_chat.Name);
            ImGui.PopStyleColor();

            if (_chat.OwnerId == APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()))
            {
                ImGui.SameLine(0, 6);

                if (ImGui.SmallButton("Edit"))
                    BeginChatNameEdit();
            }
        }

        ImGui.EndGroup();

        ImGui.SameLine();
        DrawParticipantsToggleButton();

        ImGui.Dummy(new Vector2(0, 5));
        ImGui.PushStyleColor(
            ImGuiCol.Separator,
            new Vector4(0.22f, 0.25f, 0.33f, 1f)
        );
        ImGui.Separator();
        ImGui.PopStyleColor();
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

        if (_showParticipants)
            ImGui.PushStyleColor(ImGuiCol.Button, Accent);

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 5f));

        ImGui.PushFont(Plugin.PluginInterface.UiBuilder.FontIcon);
        ImGui.SetWindowFontScale(0.90f);
        var clicked = ImGui.Button(FontAwesomeIcon.Users.ToIconString(), new Vector2(buttonSize, buttonSize));
        ImGui.SetWindowFontScale(1f);
        ImGui.PopFont();
        ImGui.PopStyleVar();

        if (_showParticipants)
            ImGui.PopStyleColor();

        if (clicked)
            _showParticipants = !_showParticipants;

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Members");
    }

    private void DrawParticipantsSidebar(float width)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.075f, 0.08f, 0.11f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);

        if (ImGui.BeginChild("##Participants", new Vector2(width, 0), true))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
            ImGui.TextUnformatted($"{_chat.Participants.Count} MEMBER{(_chat.Participants.Count == 1 ? "" : "S")}");
            ImGui.PopStyleColor();

            ImGui.Dummy(new Vector2(0, 4));
            ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.22f, 0.25f, 0.33f, 1f));
            ImGui.Separator();
            ImGui.PopStyleColor();
            ImGui.Dummy(new Vector2(0, 6));

            foreach (var participant in _chat.Participants)
            {
                var isMe = APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()) == participant.id;
                var isOwner = participant.id == _chat.OwnerId;
                var ownerTextPrefix = isOwner ? "☆ " : "";

                ImGui.PushStyleColor(ImGuiCol.Text, isMe ? Accent : Text);
                ImGui.TextWrapped(isMe
                    ? $"{ownerTextPrefix}{participant.FirstName} {participant.LastName} (you)"
                    : $"{ownerTextPrefix}{participant.FirstName} {participant.LastName}");
                ImGui.PopStyleColor();

                ImGui.Dummy(new Vector2(0, 4));
            }
        }
        ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    private void DrawMessageLog(float width)
    {
        const float inputAreaHeight = 62f;
        float logHeight = MathF.Max(80f, ImGui.GetContentRegionAvail().Y - inputAreaHeight);

        if (ImGui.BeginChild("##ChatLog", new Vector2(width, logHeight), true, ImGuiWindowFlags.HorizontalScrollbar))
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

        ImGui.EndChild();
    }

    // Compact mode: one flat line per message, no bubble background, minimal
    // vertical space. System lines just render muted instead of getting their
    // own bubble treatment.
    private void DrawMessageLineCompact(ChatMessage msg)
    {
        var isSystem = msg.Author.id == "0";
        var time = $"{msg.CreatedAt:HH:mm}";

        ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
        ImGui.TextUnformatted(time);
        ImGui.PopStyleColor();

        ImGui.SameLine(0, 6);

        if (isSystem)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted(msg.Message);
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
            return;
        }

        var isMine = APIHandler.HashString(Plugin.PlayerState.ContentId.ToString()) == msg.Author.id;
        var name = $"{msg.Author.FirstName} {msg.Author.LastName}:";

        ImGui.PushStyleColor(ImGuiCol.Text, isMine ? NameMine : NameOther);
        ImGui.TextUnformatted(name);
        ImGui.PopStyleColor();

        ImGui.SameLine(0, 5);

        ImGui.PushStyleColor(ImGuiCol.Text, Text);
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(msg.Message);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
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

        ImGui.PushStyleColor(ImGuiCol.ChildBg, isMine ? PastelPink : PastelBlue);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 12f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(paddingX, paddingY));

        string id = $"##Bubble_{msg.CreatedAt.Ticks}_{msg.Author.id}";

        if (ImGui.BeginChild(id, new Vector2(bubbleWidth, bubbleHeight), true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, isMine ? Accent : Text);
            ImGui.TextUnformatted(author);
            ImGui.PopStyleColor();

            ImGui.SameLine(0, 8);

            ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
            ImGui.TextUnformatted(time);
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, Text);
            ImGui.TextWrapped(msg.Message);
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();

        ImGui.Dummy(new Vector2(0, 6f));
    }

    private static void DrawSystemLine(ChatMessage msg)
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

        ImGui.PushStyleColor(ImGuiCol.ChildBg, PastelPink);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 12f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(paddingX, paddingY));

        string id = $"##SystemBubble_{msg.CreatedAt.Ticks}";

        if (ImGui.BeginChild(id, new Vector2(bubbleWidth, bubbleHeight), true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Text);
            ImGui.TextUnformatted(time);
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, Text);
            ImGui.TextWrapped(msg.Message);
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();

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
