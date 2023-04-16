using CalamityMod.Items.Weapons.Ranged;
using InfernumMode.Content.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class OverridesGlobalItem : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ModContent.ItemType<HalibutCannon>())
                HalibutCannonOverride.SetDefaults(item);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item.type == ModContent.ItemType<HalibutCannon>())
                return HalibutCannonOverride.Shoot(item, player, source, position, velocity, damage, damage, knockback);
            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }
    }
}
