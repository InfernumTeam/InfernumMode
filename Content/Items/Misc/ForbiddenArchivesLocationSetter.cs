using InfernumMode.Core.GlobalInstances.Systems;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using CalamityMod;

namespace InfernumMode.Content.Items.Misc
{
    public class ForbiddenArchivesLocationSetter : ModItem
    {
        public Point OldLocation
        {
            get;
            private set;
        }

        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 0;

        public override void SetDefaults()
        {
            Item.width = 300;
            Item.height = 300;
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
                    OldLocation = WorldSaveSystem.ForbiddenArchiveCenter;
                    Point mousePos = player.Calamity().mouseWorld.ToTileCoordinates();
                    Point newLocation = new(mousePos.X, mousePos.Y - 80);
                    WorldSaveSystem.ForbiddenArchiveCenter = newLocation;
                    Main.NewText($"[c/8bb564:Forbidden Archive location moved from ][c/ff2b2b:{OldLocation}][c/8bb564: to ][c/3dff62:{newLocation}][c/8bb564:.]");
                }
                else if (player.altFunctionUse == 2)
                {
                    (OldLocation, WorldSaveSystem.ForbiddenArchiveCenter) = (WorldSaveSystem.ForbiddenArchiveCenter, OldLocation);
                    Main.NewText($"[c/8bb564:Forbidden Archive location reverted to ][c/3dff62:{WorldSaveSystem.ForbiddenArchiveCenter}] [c/8bb564:from ][c/ff2b2b:{OldLocation}][c/8bb564:.]");
                }
            }
            return true;
        }

        public override void LoadData(TagCompound tag) => OldLocation = new(tag.GetInt("OldLocationX"), tag.GetInt("OldLocationY"));

        public override void SaveData(TagCompound tag)
        {
            tag["OldLocationX"] = OldLocation.X;
            tag["OldLocationY"] = OldLocation.Y;
        }
    }
}
