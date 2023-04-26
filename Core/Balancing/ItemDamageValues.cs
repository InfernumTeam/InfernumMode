using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Core.Balancing
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

            // There is no natural force, no higher power, no deity who can stop me. In the marniation of apotheotic bliss I look deep within myself.
            // All I see is an eternal and unyielding freedom.
            // Upon searching, I find the strength to conquer every obstacle and hurdle.
            // For I am the master of my fate, the captain of my soul, and I will not be denied.
            [ModContent.ItemType<Eternity>()] = 7200,
        };
    }
}
