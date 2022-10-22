using InfernumMode.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class LostColosseumTeleporter : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Lost Colosseum Teleporter");
            Tooltip.SetDefault("Brings you to the GSS arena and back. This is a test item.");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Red;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            if (SubworldSystem.IsActive<LostColosseum>())
                SubworldSystem.Exit();
            else
                SubworldSystem.Enter<LostColosseum>();

            return true;
        }
    }
}
