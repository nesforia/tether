// windows/ManageChatsWindow.cs

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Tether.modules;
using Tether.states;

namespace Tether.windows;

public class ManageChatsWindow : Window
{
    private readonly Plugin plugin;

    private static readonly Vector4 Accent = new(0.38f, 0.62f, 1.00f, 1f);
    private static readonly Vector4 AccentHover = new(0.46f, 0.70f, 1.00f, 1f);
    private static readonly Vector4 Surface = new(0.10f, 0.11f, 0.15f, 1f);
    private static readonly Vector4 Surface2 = new(0.13f, 0.14f, 0.19f, 1f);
    private static readonly Vector4 SurfaceHover = new(0.16f, 0.18f, 0.24f, 1f);
    private static readonly Vector4 Text = new(0.94f, 0.95f, 0.98f, 1f);
    private static readonly Vector4 TextMuted = new(0.55f, 0.58f, 0.66f, 1f);
    private static readonly Vector4 Green = new(0.35f, 0.86f, 0.62f, 1f);
    private static readonly Vector4 Red = new(0.95f, 0.34f, 0.40f, 1f);

    public ManageChatsWindow(Plugin plugin)
        : base("My Chats###ManageChatsWindow")
    {
        this.plugin = plugin;

        Size = new Vector2(400, 330);
        SizeCondition = ImGuiCond.FirstUseEver;

        Flags = ImGuiWindowFlags.None;
        RespectCloseHotkey = true;
    }

    public override void Draw()
    {
        IDisposable themeColor = null;
        IDisposable themeStyle = null;

        if (!plugin.Configuration.LEGACY_THEME)
        {
            themeColor = ImRaii.PushColor(ImGuiCol.WindowBg, Surface)
                                         .Push(ImGuiCol.ChildBg, Surface2)
                                         .Push(ImGuiCol.Border, new Vector4(0.20f, 0.22f, 0.29f, 1f))
                                         .Push(ImGuiCol.FrameBg, Surface2)
                                         .Push(ImGuiCol.FrameBgHovered, SurfaceHover)
                                         .Push(ImGuiCol.Button, Accent)
                                         .Push(ImGuiCol.ButtonHovered, AccentHover)
                                         .Push(ImGuiCol.ButtonActive, new Vector4(0.32f, 0.52f, 0.88f, 1f));

           themeStyle = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 8))
                                         .Push(ImGuiStyleVar.FramePadding, new Vector2(6, 4))
                                         .Push(ImGuiStyleVar.ItemSpacing, new Vector2(5, 4))
                                         .Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 3))
                                         .Push(ImGuiStyleVar.ScrollbarSize, 10f)
                                         .Push(ImGuiStyleVar.WindowRounding, 10f)
                                         .Push(ImGuiStyleVar.ChildRounding, 8f);
        }

        ImGui.Dummy(new Vector2(0, 5));

        var chats = plugin.ChatModule.Chats;

        if (chats.Count == 0)
        {
            DrawEmptyState();
        }
        else
        {
            foreach (var chat in chats.ToList())
            {
                DrawChatRow(chat);  
            }
        }
        
        themeStyle?.Dispose();
        themeColor?.Dispose();
    }

    private void DrawChatRow(GroupChat chat)
    {
        using var childBg = ImRaii.PushColor(ImGuiCol.ChildBg, Surface2, !plugin.Configuration.LEGACY_THEME);
        using var border = ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.19f, 0.21f, 0.28f, 1f), !plugin.Configuration.LEGACY_THEME);
        
        using (ImRaii.Child($"##chat_{chat.Id}", new Vector2(0, 58), true))
        {

            using (ImRaii.PushColor(ImGuiCol.Text, Text))
            { 
                ImGui.TextUnformatted(chat.Name);
            }
            
            using (ImRaii.PushColor(ImGuiCol.Text, TextMuted))
            {
                ImGui.TextUnformatted($"{chat.Participants.Count} members of this chat");
            }

            var buttonWidth = 54f;
            var buttonHeight = 32f;
            var spacing = ImGui.GetStyle().ItemSpacing.X;
            var totalButtonWidth = buttonWidth * 2 + spacing;

            var buttonY = (ImGui.GetContentRegionAvail().Y + (buttonHeight / 2f)) / 2f;
            var buttonX = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - totalButtonWidth;
            
            ImGui.SetCursorPos(new Vector2(buttonX, buttonY));
            
            using (ImRaii.PushColor(ImGuiCol.Button, Accent, !plugin.Configuration.LEGACY_THEME)
                         .Push(ImGuiCol.ButtonHovered, AccentHover, !plugin.Configuration.LEGACY_THEME)
                         .Push(ImGuiCol.ButtonActive, new Vector4(0.32f, 0.52f, 0.88f, 1f), !plugin.Configuration.LEGACY_THEME))
            {
                if (ImGui.Button("Open", new Vector2(buttonWidth, buttonHeight)))
                {
                    var window = plugin.ChatModule.FindWindow(chat.Id);
                    if (window is not null)
                        window.IsOpen = true;
                } 
            }

            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Button, Surface, !plugin.Configuration.LEGACY_THEME)
                         .Push(ImGuiCol.ButtonHovered, new Vector4(0.22f, 0.15f, 0.18f, 1f), !plugin.Configuration.LEGACY_THEME)
                         .Push(ImGuiCol.ButtonActive, new Vector4(0.28f, 0.17f, 0.20f, 1f), !plugin.Configuration.LEGACY_THEME)
                         .Push(ImGuiCol.Text, Red, !plugin.Configuration.LEGACY_THEME))
            {
                if (ImGui.Button("Leave", new Vector2(buttonWidth, buttonHeight)))
                {
                    _ = APIHandler.SendApiRequest("/group/leave", new { id = chat.Id });
                    plugin.ChatModule.RemoveGroup(chat.Id);
                } 
            }

        }

        ImGui.Dummy(new Vector2(0, 3));
    }

    private static void DrawEmptyState()
    {
        ImGui.Dummy(new Vector2(0, 45));

        var availableWidth = ImGui.GetContentRegionAvail().X;

        CenterText("No active chats", Text, availableWidth);

        ImGui.Dummy(new Vector2(0, 8));

        CenterText("Right-click a friend to start a temporary group.", TextMuted, availableWidth);

        ImGui.Dummy(new Vector2(0, 5));

        CenterText("Chats disappear when you leave them.", TextMuted, availableWidth);
    }

    private static void CenterText(string text, Vector4 color, float availableWidth)
    {
        var size = ImGui.CalcTextSize(text);
        var offset = MathF.Max(0f, (availableWidth - size.X) * 0.5f);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(text);
        }
    }
}
