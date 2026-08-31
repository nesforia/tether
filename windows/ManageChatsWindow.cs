// windows/ManageChatsWindow.cs

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
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
        PushStyle();   // push 8 vars + 8 colors

        DrawHeader();

        ImGui.Dummy(new Vector2(0, 5));

        var chats = plugin.ChatModule.Chats;

        if (chats.Count == 0)
            DrawEmptyState();
        else
        {
            foreach (var chat in chats.ToList())
                DrawChatRow(chat);
        }

        ImGui.PopStyleColor(8);
        ImGui.PopStyleVar(8);
    }


    private static void PushStyle()
    {
        // Kompaktowy styl – tylko lokalnie
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 8));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6f);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Surface);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Surface2);
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.20f, 0.22f, 0.29f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Surface2);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, SurfaceHover);
        ImGui.PushStyleColor(ImGuiCol.Button, Accent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, AccentHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.32f, 0.52f, 0.88f, 1f));
    }

    private static void DrawHeader()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Text);
        ImGui.TextUnformatted("Your chats");
        ImGui.PopStyleColor();

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
        ImGui.TextUnformatted("  ·  temporary groups");
        ImGui.PopStyleColor();

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 38f);

        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.22f, 0.25f, 0.33f, 1f));
        ImGui.Separator();
        ImGui.PopStyleColor();

        ImGui.Dummy(new Vector2(0, 4));
    }

    private void DrawChatRow(GroupChat chat)
    {
        ImGui.PushID(chat.Id);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, Surface2);
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.19f, 0.21f, 0.28f, 1f));

        if (ImGui.BeginChild($"##chat_{chat.Id}", new Vector2(0, 58), true))
        {
            var draw = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();

            // Accent strip.
            draw.AddRectFilled(
                pos,
                pos + new Vector2(4, ImGui.GetWindowHeight()),
                ImGui.GetColorU32(Accent),
                4f);

            ImGui.SameLine(0, 7);

            ImGui.PushStyleColor(ImGuiCol.Text, Text);
            ImGui.TextUnformatted(chat.Name);
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, TextMuted);
            ImGui.TextUnformatted("temporary");
            ImGui.PopStyleColor();

            var buttonWidth = 54f;
            var spacing = ImGui.GetStyle().ItemSpacing.X;
            var totalButtonWidth = buttonWidth * 2 + spacing;

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - totalButtonWidth);

            ImGui.PushStyleColor(ImGuiCol.Button, Accent);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, AccentHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.32f, 0.52f, 0.88f, 1f));

            if (ImGui.Button("Open", new Vector2(buttonWidth, 32)))
            {
                var window = plugin.ChatModule.FindWindow(chat.Id);
                if (window is not null)
                    window.IsOpen = true;
            }

            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, Surface);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.22f, 0.15f, 0.18f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.28f, 0.17f, 0.20f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, Red);

            if (ImGui.Button("Leave", new Vector2(buttonWidth, 32)))
            {
                _ = APIHandler.SendPOST("/group/leave", new { id = chat.Id });
                plugin.ChatModule.RemoveGroup(chat.Id);
            }

            ImGui.PopStyleColor(4);
        }

        ImGui.EndChild();

        ImGui.PopStyleColor(2);
        ImGui.PopID();

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

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }
}
