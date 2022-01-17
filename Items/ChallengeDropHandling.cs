using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public static class ChallengeDropHandling
    {
        public static Dictionary<int, int[]> ChallengeDropTable => new Dictionary<int, int[]>()
        {
            [NPCID.KingSlime] = new int[] { ModContent.ItemType<CrownJewel>() },
            [ModContent.NPCType<DesertScourgeHead>()] = new int[] { ModContent.ItemType<DuneHopper>() },
            [NPCID.EyeofCthulhu] = new int[] { ModContent.ItemType<TeardropCleaver>(), ModContent.ItemType<CounterScarf>() },
            [ModContent.NPCType<CrabulonIdle>()] = new int[] { ModContent.ItemType<TheTransformer>() },
            [ModContent.NPCType<HiveMind>()] = new int[] { ModContent.ItemType<Carnage>() },
            [ModContent.NPCType<PerforatorHive>()] = new int[] { ModContent.ItemType<Carnage>() },
            [NPCID.QueenBee] = new int[] { ModContent.ItemType<TheBee>() },
            [NPCID.SkeletronHead] = new int[] { ModContent.ItemType<ClothiersWrath>() },
            [NPCID.WallofFlesh] = new int[] { ModContent.ItemType<EvilSmasher>() },
            [ModContent.NPCType<Cryogen>()] = new int[] { ModContent.ItemType<Cryophobia>(), ModContent.ItemType<ColdDivinity>() },
            [NPCID.Spazmatism] = new int[] { ModContent.ItemType<Arbalest>() },
            [NPCID.Retinazer] = new int[] { ModContent.ItemType<Arbalest>() },
            [ModContent.NPCType<AquaticScourgeHead>()] = new int[] { ModContent.ItemType<DeepDiver>(), ModContent.ItemType<SeasSearing>() },
            //[NPCID.SkeletronPrime] = new int[] { ModContent.ItemType<GoldBurdenBreaker>(), ModContent.ItemType<SpearofDestiny>() },
            [ModContent.NPCType<BrimstoneElemental>()] = new int[] { ModContent.ItemType<Hellborn>(), ModContent.ItemType<FabledTortoiseShell>() },
            [NPCID.TheDestroyer] = new int[] { ModContent.ItemType<SHPC>() },
            [ModContent.NPCType<CalamitasRun3>()] = new int[] { ModContent.ItemType<Regenator>() },
            [NPCID.Plantera] = new int[] { ModContent.ItemType<ThornBlossom>(), ModContent.ItemType<BlossomFlux>() },
            [ModContent.NPCType<Leviathan>()] = new int[] { ModContent.ItemType<TheCommunity>() },
            [ModContent.NPCType<Siren>()] = new int[] { ModContent.ItemType<TheCommunity>() },
            [ModContent.NPCType<AstrumAureus>()] = new int[] { ModContent.ItemType<LeonidProgenitor>() },
            //[NPCID.Golem] = new int[] { ModContent.ItemType<AegisBlade>(), ModContent.ItemType<LeadWizard>() },
            [ModContent.NPCType<PlaguebringerGoliath>()] = new int[] { ModContent.ItemType<Malachite>() },
            [NPCID.DukeFishron] = new int[] { ModContent.ItemType<BrinyBaron>() },
            [ModContent.NPCType<RavagerBody>()] = new int[] { ModContent.ItemType<Vesuvius>() },
            [NPCID.CultistBoss] = new int[] { ModContent.ItemType<EyeofMagnus>() },
            [ModContent.NPCType<AstrumDeusHeadSpectral>()] = new int[] { ModContent.ItemType<HideofAstrumDeus>(), ModContent.ItemType<Quasar>(), ModContent.ItemType<TrueConferenceCall>() },
            //[NPCID.MoonLordCore] = new int[] { ModContent.ItemType<GrandDad>(), ModContent.ItemType<Infinity>() },
            [ModContent.NPCType<Bumblefuck>()] = new int[] { ModContent.ItemType<Swordsplosion>() },
            [ModContent.NPCType<Providence>()] = new int[] { ModContent.ItemType<SamuraiBadge>(), ModContent.ItemType<PristineFury>() },
            [ModContent.NPCType<CeaselessVoid>()] = new int[] { ModContent.ItemType<TheEvolution>() },
            [ModContent.NPCType<StormWeaverHead>()] = new int[] { ModContent.ItemType<Thunderstorm>() },
            [ModContent.NPCType<Signus>()] = new int[] { ModContent.ItemType<LanternoftheSoul>() },
            [ModContent.NPCType<Polterghast>()] = new int[] { ModContent.ItemType<PearlGod>() },
            [ModContent.NPCType<OldDuke>()] = new int[] { ModContent.ItemType<TheReaper>() },
            //[ModContent.NPCType<DevourerofGodsHead>()] = new int[] { ModContent.ItemType<Norfleet>(), ModContent.ItemType<Skullmasher>(), ModContent.ItemType<CosmicDischarge>() },
            [ModContent.NPCType<Yharon>()] = new int[] { ModContent.ItemType<YharimsCrystal>(), ModContent.ItemType<VoidVortex>() },
            [ModContent.NPCType<SupremeCalamitas>()] = new int[] { ModContent.ItemType<GaelsGreatsword>() },
        };
    }
}
