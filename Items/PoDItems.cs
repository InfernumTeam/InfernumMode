using CalamityMod.Items.Ammo;
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
    }
}
