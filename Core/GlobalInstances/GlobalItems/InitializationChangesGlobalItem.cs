using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using InfernumMode.Core.Balancing;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class InitializationChangesGlobalItem : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.CelestialSigil)
            {
                item.consumable = false;
                item.maxStack = 1;
            }

            bool isGSSItem = item.type == ModContent.ItemType<GrandScale>() || item.type == ModContent.ItemType<DuststormInABottle>() || item.type == ModContent.ItemType<SandSharknadoStaff>() ||
                item.type == ModContent.ItemType<ShiftingSands>() || item.type == ModContent.ItemType<Tumbleweed>() ||
                item.type == ModContent.ItemType<SandSharkToothNecklace>() || item.type == ModContent.ItemType<SandstormsCore>();

            if (isGSSItem)
                item.rare = ItemRarityID.Cyan;

            if (ItemDamageValues.DamageValues.TryGetValue(item.type, out int newDamage))
                item.damage = newDamage;
        }
    }
}
