using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace TeleportNPCLocation
{
    internal class ModConfig
    {
        /// <summary>The keys which toggle the lookup UI for something under the cursor.</summary>
        public KeybindList ToggleNPCMenu { get; set; } = new(SButton.P);

        public bool ExampleCheckbox { get; set; } = true;
    }

}
