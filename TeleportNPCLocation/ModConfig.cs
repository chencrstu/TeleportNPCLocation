using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace TeleportNPCLocation
{
    public class ModConfig
    {
        /// <summary>The keys which toggle npc menu.</summary>
        public KeybindList ToggleNPCMenu { get; set; } = new(SButton.OemTilde);

        /// <summary>show more npc info.</summary>
        public bool showMoreInfo { get; set; } = true;
    }
}
