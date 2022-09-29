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
            [ModContent.ItemType<StickySpikyBall>()] = 6,
            [ModContent.ItemType<NapalmArrow>()] = 9,
            [ModContent.ItemType<MeteorFist>()] = 10,
            [ItemID.StarCannon] = 24,
            [ModContent.ItemType<SeasSearing>()] = 31,
            [ModContent.ItemType<HivePod>()] = 74,
            [ModContent.ItemType<HeavenfallenStardisk>()] = 87,
            [ModContent.ItemType<Atlantis>()] = 55,
            [ModContent.ItemType<Malachite>()] = 67,
            [ModContent.ItemType<ResurrectionButterfly>()] = 44,
            [ModContent.ItemType<Lazhar>()] = 42,
            [ModContent.ItemType<Omniblade>()] = 54,
            [ModContent.ItemType<BrinyBaron>()] = 65,
            [ModContent.ItemType<WavePounder>()] = 47,
            [ModContent.ItemType<TerrorBlade>()] = 495,
            [ModContent.ItemType<SoulEdge>()] = 140,
            [ModContent.ItemType<NightsGaze>()] = 396,
            [ModContent.ItemType<Valediction>()] = 139,
            [ModContent.ItemType<TheSevensStriker>()] = 543,
            [ModContent.ItemType<PhosphorescentGauntlet>()] = 990,
            [ModContent.ItemType<PridefulHuntersPlanarRipper>()] = 48,
            [ModContent.ItemType<FinalDawn>()] = 855,
            [ModContent.ItemType<DragonsBreath>()] = 132,
            [ModContent.ItemType<TyrannysEnd>()] = 1540,
        };
    }
}
