using CalamityMod.Items.Ammo;
using CalamityMod.Items.Weapons.Magic;
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
            [ModContent.ItemType<StickySpikyBall>()] = 6,
            [ModContent.ItemType<NapalmArrow>()] = 9,
            [ModContent.ItemType<MeteorFist>()] = 10,
            [ItemID.StarCannon] = 24,
            [ModContent.ItemType<SeasSearing>()] = 39,
            [ModContent.ItemType<HivePod>()] = 74,
            [ModContent.ItemType<HeavenfallenStardisk>()] = 87,
            [ModContent.ItemType<Atlantis>()] = 55,
            [ModContent.ItemType<ResurrectionButterfly>()] = 44,
            [ModContent.ItemType<FinalDawn>()] = 855,
        };
    }
}
