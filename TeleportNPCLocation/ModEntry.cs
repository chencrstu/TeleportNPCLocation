using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using TeleportNPCLocation.framework;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

// 1. 获取npc的位置 done
// 2. 定位可传送位置，碰撞检测 doing
// 3. 实现传送能力 done
// 4. 定制物品，贴图，售价，售出位置 // Content Patcher https://stardewvalleywiki.com/Modding:Content_Patcher

namespace TeleportNPCLocation
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        // shoud find npc
        private string  findNPCName = "Emily";
        private readonly string[] NPCNames = { "Robin", "Shane", "George", "Evelyn", "Alex", "Haley", "Emily", "Jodi", "Vincent", "Sam", "Clint", "Pierre", "Caroline", "Abigail", "Gus", "Willy", "Maru", "Demetrius", "Sebastian", "Linus", "Marnie", "Jas", "Leah", "Dwarf", "Bouncer", "Gunther", "Marlon", "Henchman", "Birdie", "Mister Qi" };

        /// <summary>The previous menus shown before the current lookup UI was opened.</summary>
        private readonly PerScreen<Stack<IClickableMenu>> PreviousMenus = new(() => new());

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;

            helper.ConsoleCommands.Add("teleport_setname", "Sets teleport to npc's name.\n\nUsage: teleport_setname <value>\n- value: the npc name in below list.\n" + string.Join("\n", this.NPCNames), this.SetFindNPCName);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Set the player's money when the 'player_setmoney' command is invoked.</summary>
        /// <param name="command">The name of the command invoked.</param>
        /// <param name="args">The arguments received by the command. Each word after the command name is a separate argument.</param>
        private void SetFindNPCName(string command, string[] args)
        {
            this.findNPCName = args[0];
            this.Monitor.Log($"OK, set teleport to npc's name: {args[0]}.", LogLevel.Info);
        }

        /// <inheritdoc cref="IDisplayEvents.MenuChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            // restore the previous menu if it was hidden to show the lookup UI
            if (e.NewMenu == null && (e.OldMenu is NPCMenu) && this.PreviousMenus.Value.Any())
                Game1.activeClickableMenu = this.PreviousMenus.Value.Pop();
        }

        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Maps/Town"))
            {
                e.Edit(asset =>
                {
                    IAssetDataForMap editor = asset.AsMap();
                    Map map = editor.Data;

                    // your code here
                });
            }
        }

        /// <summary>Get a tile from the map.</summary>
        /// <param name="map">The map instance.</param>
        /// <param name="layerName">The name of the layer from which to get a tile.</param>
        /// <param name="tileX">The X position measured in tiles.</param>
        /// <param name="tileY">The Y position measured in tiles.</param>
        /// <returns>Returns the tile if found, else <c>null</c>.</returns>
        private Tile GetTile(Map map, string layerName, int tileX, int tileY)
        {
            Layer layer = map.GetLayer(layerName);
            Location pixelPosition = new Location(tileX * Game1.tileSize, tileY * Game1.tileSize);

            return layer.PickTile(pixelPosition, Game1.viewport.Size);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);


            if (!e.Button.ToString().Equals("P"))
                return;

            // try toggle npc menu list
            TryToggleNPCMenu();

        }

        private static IEnumerable<GameLocation> GetAllStaticLocations()
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );
        }

        /// <summary>Get only relevant villagers for the world map.</summary>
        private static List<NPC> GetVillagers()
        {
            var villagers = new List<NPC>();

            foreach (GameLocation location in GetAllStaticLocations())
            {
                foreach (var npc in location.characters)
                {
                    if (npc != null && !villagers.Contains(npc) && npc.isVillager())
                        villagers.Add(npc);
                }
            }

            return villagers;
        }

        private NPC getFindNPCInfo()
        {
            List<NPC> villagers = GetVillagers();
            NPC findNPC = null;

            foreach (var npc in villagers)
            {
                if (npc.currentLocation == null)
                {
                    continue;
                }

                string locationName = npc.currentLocation.uniqueName.Value ?? npc.currentLocation.Name;
                GameLocation location = npc.currentLocation;

                //this.Monitor.Log($"npc name:{npc.Name}", LogLevel.Debug);

                if (npc.Name.Equals(this.findNPCName))
                {
                    findNPC = npc;
                    string result = $"name:{npc.Name}, displayName:{npc.displayName}, location:{locationName}, x:{npc.getTileX()}, y: {npc.getTileY()}, birthday: {npc.Birthday_Day}, gender: {npc.Gender}, age: {npc.Age}\n";
                    this.Monitor.Log($"find npc info:{result}", LogLevel.Debug);
                    break;
                }

            }

            return findNPC;
        }

        /****
        ** NPC menu helpers
        ****/
        /// <summary>Toggle the npc UI if applicable.</summary>
        private void TryToggleNPCMenu()
        {
            if (Game1.activeClickableMenu is NPCMenu)
                this.hideNPCMenu();
            else if (Context.IsWorldReady && Game1.activeClickableMenu is not NPCMenu)
                this.showNPCMenu();
        }

        private void hideNPCMenu()
        {
            if (Game1.activeClickableMenu is NPCMenu)
            {
                Game1.playSound("bigDeSelect"); // match default behaviour when closing a menu
                Game1.activeClickableMenu = null;
            }
        }

        private void showNPCMenu()
        {
            StringBuilder logMessage = new("Received a npc list request...");
            this.Monitor.InterceptErrors("fetch npc list", () =>
            {
                List<NPC> villagers = GetVillagers();

                this.PushMenu(new NPCMenu(npcList: villagers, monitor: this.Monitor, scroll: 160));

            });
        }

        private void PushMenu(IClickableMenu menu)
        {
            if (this.ShouldRestoreMenu(Game1.activeClickableMenu))
            {
                this.PreviousMenus.Value.Push(Game1.activeClickableMenu);
                this.Helper.Reflection.GetField<IClickableMenu>(typeof(Game1), "_activeClickableMenu").SetValue(menu); // bypass Game1.activeClickableMenu, which disposes the previous menu
            }
            else
                Game1.activeClickableMenu = menu;
        }

        /// <summary>Get whether a given menu should be restored when the lookup ends.</summary>
        /// <param name="menu">The menu to check.</param>
        private bool ShouldRestoreMenu(IClickableMenu? menu)
        {
            // no menu
            if (menu == null)
                return false;

            return true;
        }
    }
}

