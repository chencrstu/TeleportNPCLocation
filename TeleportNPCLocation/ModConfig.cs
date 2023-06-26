using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace TeleportNPCLocation
{
    internal class ModConfig
    {
        /// <summary>The keys which toggle npc menu.</summary>
        public KeybindList ToggleNPCMenu { get; set; } = new(SButton.P);
    }
}
