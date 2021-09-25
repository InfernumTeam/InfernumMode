using CalamityMod;
using CalamityMod.Items.Ammo;
using CalamityMod.Items.TreasureBags;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class PoDItems : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ModContent.ItemType<FlashBullet>())
                item.damage = 4;

            if (item.type == ItemID.StarCannon)
                item.damage = 24;
        }

        public override void RightClick(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<StarterBag>())
                DropHelper.DropItemCondition(player, ModContent.ItemType<Death2>(), Main.expertMode);
        }
    }
}
