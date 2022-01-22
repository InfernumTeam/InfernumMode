using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Ammo;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Summon;

namespace InfernumMode.Balancing
{
    public static class ItemDamageValues
    {
        public static Dictionary<int, int> DamageValues => new Dictionary<int, int>()
        {
            [ModContent.ItemType<StickySpikyBall>()] = 6,
            [ModContent.ItemType<FlashBullet>()] = 4,
            [ModContent.ItemType<NapalmArrow>()] = 9,
            [ModContent.ItemType<MeteorFist>()] = 10,
            [ItemID.StarCannon] = 24,
            [ModContent.ItemType<SeasSearing>()] = 39,
            [ModContent.ItemType<HivePod>()] = 74,
            [ModContent.ItemType<HeavenfallenStardisk>()] = 87,
            [ModContent.ItemType<ResurrectionButterfly>()] = 44,
            [ModContent.ItemType<FinalDawn>()] = 855,
        };
    }
}
