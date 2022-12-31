using CalamityMod.Items.Ammo;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Balancing
{
    public static class ItemDamageValues
    {
        public static Dictionary<int, int> DamageValues => new()
        {
            // GSS is moved to post LC, and these are buffed as a result.
            [ModContent.ItemType<DuststormInABottle>()] = 72,
            [ModContent.ItemType<SandstormGun>()] = 145,
            [ModContent.ItemType<SandSharknadoStaff>()] = 59,
            [ModContent.ItemType<Sandslasher>()] = 196,
            [ModContent.ItemType<ShiftingSands>()] = 104,
            [ModContent.ItemType<Tumbleweed>()] = 203,
        };
    }
}
