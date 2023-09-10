using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod;
using CalamityMod.Schematics;
using InfernumMode.Content.Schematics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Items.Misc
{
    public class ProfanedGardenLocationSetter : ModItem
    {
        public Rectangle OldLocation
        {
            get;
            private set;
        }

        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 0;

        public override void SetDefaults()
        {
            Item.width = Item.height = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.noMelee = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (player.altFunctionUse != 2)
                {
                    // Store the old position.
                    OldLocation = WorldSaveSystem.ProvidenceArena;
                    Point mousePos = player.Calamity().mouseWorld.ToTileCoordinates();
                    // For ease of use, the player needs only select the middle of the base of the plaque tile. Due to that, the position is manually offset
                    // so that its back at the top left point of the garden.
                    mousePos -= new Point(272, 120);
                    SchematicMetaTile[,] schematic = InfernumSchematicLoaderSystem.TileMaps["Profaned Arena"];
                    int width = schematic.GetLength(0);
                    int height = schematic.GetLength(1);
                    Rectangle newLocation = new(mousePos.X, mousePos.Y, width, height);
                    WorldSaveSystem.ProvidenceArena = newLocation;
                    Main.NewText($"[c/ffe38c:Profaned Garden location moved from ][c/ff2b2b:{OldLocation}][c/ffe38c: to ][c/3dff62:{newLocation}][c/ffe38c:.]");
                }
                else if (player.altFunctionUse == 2)
                {
                    (OldLocation, WorldSaveSystem.ProvidenceArena) = (WorldSaveSystem.ProvidenceArena, OldLocation);
                    Main.NewText($"[c/ffe38c:Profaned Garden location reverted to ][c/3dff62:{WorldSaveSystem.ProvidenceArena}] [c/ffe38c:from ][c/ff2b2b:{OldLocation}][c/ffe38c:.]");
                }
            }
            return true;
        }

        public override void LoadData(TagCompound tag) => OldLocation = tag.Get<Rectangle>("OldLocation");

        public override void SaveData(TagCompound tag) => tag["OldLocation"] = OldLocation;
    }
}
