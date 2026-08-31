using Dalamud.Configuration;
using System;

namespace Tether;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // # REQUEST SETTINGS
    public bool DECLINE_EVERY_CHAT_REQUEST { get; set; } = false;
    public bool BLOCK_INVITES_WHILE_IN_DUTY { get; set; } = false;
    
    // # CHAT SETTINGS
    public bool DISPLAY_NOTIFICATIONS_ON_NEW_MESSAGE { get; set; } = true;
    public bool SOUND_NOTIFICATION_ON_NEW_MESSAGE { get; set; } = true;
    public bool AUTO_OPEN_WINDOW_ON_NEW_CHAT { get; set; } = true;
    public float OPACITY_WINDOW_CHAT_ON_UNFOCUSED { get; set; } = 0.5f;
    
    // # MESSAGES SETTINGS
    public bool COMPACT_CHAT_MODE { get; set; } = false;
    

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
