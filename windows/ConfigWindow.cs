// windows/ConfigWindow.cs
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace Tether.windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;

    private static readonly Vector4 Accent      = new(0.38f, 0.62f, 1.00f, 1f);
    private static readonly Vector4 AccentSoft  = new(0.28f, 0.42f, 0.72f, 1f);
    private static readonly Vector4 Surface     = new(0.10f, 0.11f, 0.15f, 1f);
    private static readonly Vector4 Surface2    = new(0.13f, 0.14f, 0.19f, 1f);
    private static readonly Vector4 TabActive   = new(0.16f, 0.18f, 0.24f, 1f);
    private static readonly Vector4 TabHovered  = new(0.20f, 0.24f, 0.32f, 1f);
    private static readonly Vector4 Text        = new(0.94f, 0.95f, 0.98f, 1f);
    private static readonly Vector4 TextMuted   = new(0.55f, 0.58f, 0.66f, 1f);

    public ConfigWindow(Configuration config)
        : base("Tether Settings###ConfigWindow")
    {
        _config = config;

        Size = new Vector2(400, 320);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ApplyWindowStyle();

        if (ImGui.BeginTabBar("##ConfigTabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Requests"))
            {
                ImGui.Dummy(new Vector2(0, 4));
                DrawRequestsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Chat"))
            {
                ImGui.Dummy(new Vector2(0, 4));
                DrawChatTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Messages"))
            {
                ImGui.Dummy(new Vector2(0, 4));
                DrawMessagesTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        EndWindowStyle();
    }

    private void DrawRequestsTab()
    {
        var declineAll = _config.DECLINE_EVERY_CHAT_REQUEST;
        if (Checkbox("Decline every chat request", ref declineAll,
                "Automatically decline all incoming invites, from anyone."))
        {
            _config.DECLINE_EVERY_CHAT_REQUEST = declineAll;
            _config.Save();
        }

        if (!declineAll)
        {
            var declineDuty = _config.BLOCK_INVITES_WHILE_IN_DUTY;
            if (Checkbox("Decline chat requests while in duty", ref declineDuty,
                         "Automatically decline all incoming invites while in duty, from anyone."))
            {
                _config.BLOCK_INVITES_WHILE_IN_DUTY = declineDuty;
                _config.Save();
            }
        }

        if (declineAll)
        {
            ImGui.Dummy(new Vector2(0, 6));
            ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted("All incoming invites are being declined automatically.");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
        }
    }

    private void DrawChatTab()
    {
        var showNotif = _config.DISPLAY_NOTIFICATIONS_ON_NEW_MESSAGE;
        if (Checkbox("Show notification on new message", ref showNotif,
                "Pop up a toast when a message arrives in an unfocused chat."))
        {
            _config.DISPLAY_NOTIFICATIONS_ON_NEW_MESSAGE = showNotif;
            _config.Save();
        }

        var playSound = _config.SOUND_NOTIFICATION_ON_NEW_MESSAGE;
        if (Checkbox("Play sound on new message", ref playSound,
                "Play a short sound alongside the notification."))
        {
            _config.SOUND_NOTIFICATION_ON_NEW_MESSAGE = playSound;
            _config.Save();
        }
        
        var openChatOnNewMessage = _config.AUTO_OPEN_WINDOW_ON_NEW_CHAT;
        if (Checkbox("Auto open chat on new message", ref openChatOnNewMessage,
                     "Automatically opens chat when new message arrives to chat."))
        {
            _config.AUTO_OPEN_WINDOW_ON_NEW_CHAT = openChatOnNewMessage;
            _config.Save();
        }

        ImGui.Dummy(new Vector2(0, 8));

        ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
        ImGui.TextUnformatted("Unfocused window opacity");
        ImGui.PopStyleColor();

        var opacityPercent = _config.OPACITY_WINDOW_CHAT_ON_UNFOCUSED * 100f;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("##OpacitySlider", ref opacityPercent, 10f, 100f, "%.0f%%",
                              ImGuiSliderFlags.AlwaysClamp))
        {
            _config.OPACITY_WINDOW_CHAT_ON_UNFOCUSED = opacityPercent / 100f;
            _config.Save();
        }
    }

    private void DrawMessagesTab()
    {
        var compact = _config.COMPACT_CHAT_MODE;
        if (Checkbox("Compact mode", ref compact,
                "Denser message layout — smaller spacing, no bubbles."))
        {
            _config.COMPACT_CHAT_MODE = compact;
            _config.Save();
        }
    }

    private static bool Checkbox(string label, ref bool value, string tooltip)
    {
        var changed = ImGui.Checkbox(label, ref value);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
            ImGui.PushStyleColor(ImGuiCol.Text, Text);
            ImGui.TextUnformatted(tooltip);
            ImGui.PopStyleColor();
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        return changed;
    }

    private void ApplyWindowStyle()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14, 12));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 8));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 6f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 6f);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Surface);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Surface2);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.16f, 0.18f, 0.24f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.20f, 0.27f, 1f));
        ImGui.PushStyleColor(ImGuiCol.CheckMark, Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, AccentSoft);
        ImGui.PushStyleColor(ImGuiCol.Tab, Surface2);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, TabHovered);
        ImGui.PushStyleColor(ImGuiCol.TabActive, TabActive);
        ImGui.PushStyleColor(ImGuiCol.Text, Text);
    }

    private static void EndWindowStyle()
    {
        ImGui.PopStyleColor(11);
        ImGui.PopStyleVar(6);
    }
}
