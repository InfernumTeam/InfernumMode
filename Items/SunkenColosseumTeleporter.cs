using InfernumMode.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class SunkenColosseumTeleporter : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Sunken Colosseum Teleporter");
            Tooltip.SetDefault("Brings you to the GSS arena and back. This is a test item.");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Red;
            Item.useAnimation = 42;
            Item.useTime = 42;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            if (SubworldSystem.IsActive<SunkenColosseum>())
                SubworldSystem.Exit();
            else
                SubworldSystem.Enter<SunkenColosseum>();

            return true;
        }
    }
}
