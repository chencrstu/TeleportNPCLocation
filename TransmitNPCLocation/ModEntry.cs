using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

// 1. 获取npc的位置 done
// 2. 定位可传送位置，碰撞检测 doing
// 3. 实现传送能力 done
// 4. 定制物品，贴图，售价，售出位置 // Content Patcher https://stardewvalleywiki.com/Modding:Content_Patcher

namespace TransmitNPCLocation
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        // shoud find npc
        private string  findNPCName = "Emily";
        private readonly string[] NPCNames = { "Robin", "Shane", "George", "Evelyn", "Alex", "Haley", "Emily", "Jodi", "Vincent", "Sam", "Clint", "Pierre", "Caroline", "Abigail", "Gus", "Willy", "Maru", "Demetrius", "Sebastian", "Linus", "Marnie", "Jas", "Leah", "Dwarf", "Bouncer", "Gunther", "Marlon", "Henchman", "Birdie", "Mister Qi" };

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            helper.ConsoleCommands.Add("transmit_setname", "Sets transmit to npc's name.\n\nUsage: transmit_setname <value>\n- value: the npc name in below list.\n" + string.Join("\n", this.NPCNames), this.SetFindNPCName);
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
            this.Monitor.Log($"OK, set transmit to npc's name: {args[0]}.", LogLevel.Info);
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

            if (findNPC != null)
            {
                TeleportToNPCLocation(findNPC);
            }
            else
            {
                this.Monitor.Log($"Can't find npc name:{this.findNPCName}", LogLevel.Debug);
            }

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

        private void TeleportToNPCLocation(NPC npc)
        {
            // Get npc location
            GameLocation location = npc.currentLocation;
            //GameLocation Location = Utility.fuzzyLocationSearch(locationName);
            if (location == null)
            {
                this.Monitor.Log($"Can't find npc location:{this.findNPCName}", LogLevel.Debug);
                return;
            }

            DelayedAction.delayedBehavior teleportFunction = delegate {
                //Insert here the coordinates you want to teleport to
                int[] offset = FindNPCAroundSpace(location, npc);

                int X = npc.getTileX() + offset[0];
                int Y = npc.getTileY() + offset[1];

                // The direction you want the Farmer to face after the teleport
                // 0 = up, 1 = right, 2 = down, 3 = left
                int direction = offset[2];

                // The teleport command itself
                Game1.warpFarmer(new LocationRequest(location.NameOrUniqueName, location.uniqueName.Value != null, location), X, Y, direction);
            };


            // Delayed action to be executed after a set time (here 0,1 seconds)
            // Teleporting without the delay may prove to be problematic
            DelayedAction.functionAfterDelay(teleportFunction, 100);
        }

        private int[] FindNPCAroundSpace(GameLocation location, NPC npc)
        {
            // Define offset array
            int[,] tileOffset = { { -1, 0 , 1}, { 1, 0, 3}, { 0, -1, 2 }, { 0, 1, 0 }, { -1, -1, 1 }, { 1, -1, 3 }, { -1, 1, 1 }, { 1, 1, 3 } };
            int[] result = { 0, 0, 0 };

            for (int i = 0; i < tileOffset.GetLength(0); i++)
            {
                int xTile = npc.getTileX() + tileOffset[i, 0];
                int yTile = npc.getTileY() + tileOffset[i, 1];
                this.Monitor.Log($"tieleOffsetX:{tileOffset[i, 0]},tieleOffsetY:{tileOffset[i, 1]}", LogLevel.Debug);

                if (location.doesTileHaveProperty(xTile, yTile, "Water", "Back") != null)
                    continue;

                if (location.doesTileHaveProperty(xTile, yTile, "Passable", "Buildings") == null)
                    continue;
                
                result = new int[]{ tileOffset[i, 0], tileOffset[i, 1], tileOffset[i, 2] };
                
            }

            this.Monitor.Log($"find npc around space:{result[0]},{result[1]},{result[2]}", LogLevel.Debug);

            return result;
        }
    }
}

