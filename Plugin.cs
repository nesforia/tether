using System.Linq;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons;
using Tether.modules;
using Tether.windows;

namespace Tether;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

    public readonly Chat ChatModule;
    private readonly APIHandler apiHandler;
    public Configuration Configuration { get; init; }
    private ManageChatsWindow ManageChatsWindow { get; init; }
    private ConfigWindow ConfigWindow { get; init; }

    public readonly WindowSystem WindowSystem = new("Tether");


    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ManageChatsWindow = new ManageChatsWindow(this);
        ConfigWindow = new ConfigWindow(Configuration);
        
        WindowSystem.AddWindow(ManageChatsWindow);
        WindowSystem.AddWindow(ConfigWindow);
        
        CommandManager.AddHandler("/tether", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the group chat manager."
        });
        CommandManager.AddHandler("/groupchats", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the group chat manager."
        });
        CommandManager.AddHandler("/tethersettings", new CommandInfo((command, args) =>
        {
            ConfigWindow.IsOpen = true;
        })
        {
            HelpMessage = "Open config."
        });
        
        ChatModule = new Chat(WindowSystem, this);
        apiHandler = new APIHandler(WindowSystem, this);
        
        PluginInterface.UiBuilder.OpenConfigUi += () => ConfigWindow.IsOpen = true;
        PluginInterface.UiBuilder.OpenMainUi += () => ManageChatsWindow.IsOpen = true;

        Framework.Update += OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw += OnDraw;
        ContextMenu.OnMenuOpened += ChatModule.DrawContextMenu;
        
        PluginInterface.UiBuilder.OpenConfigUi += () => ConfigWindow.IsOpen = true;
        
        ECommonsMain.Init(PluginInterface, this);
    }
    
    private void OnFrameworkUpdate(IFramework framework)
    {
        if (PlayerState.IsLoaded)
        {
            _ = apiHandler.GenerateUserToken();
        }
    }
    
    private void OnCommand(string command, string args)
    {
        ToggleManageChatUi();
    }
    
    private void OnDraw()
    {
        WindowSystem.Draw();
    }

    public void Dispose()
    {
        if (ChatModule.Chats.ToList().Count > 0)
        {
            ChatModule.Chats.ToList().Each(chat =>
            {
                ChatModule.RemoveGroup(chat.Id);
                _ = APIHandler.SendPOST("/group/leave", new { id = chat.Id });
            });
        }
        
        WindowSystem.RemoveAllWindows();
        
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        ContextMenu.OnMenuOpened -= ChatModule.DrawContextMenu;
        
        ECommonsMain.Dispose();
        ChatModule.Dispose();
        _ = apiHandler.Disconnect();

        CommandManager.RemoveHandler("/groupchats");
        CommandManager.RemoveHandler("/tether");
        CommandManager.RemoveHandler("/tethersettings");
    }

    public void ToggleManageChatUi() => ManageChatsWindow.Toggle();
}
