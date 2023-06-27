using CalamityMod;
using InfernumMode.Content.Items;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Items.Dyes;
using InfernumMode.Content.Items.Pets;
using InfernumMode.Content.Items.Weapons.Magic;
using InfernumMode.Content.Items.Weapons.Melee;
using InfernumMode.Content.Items.Weapons.Ranged;
using InfernumMode.Content.Items.Weapons.Rogue;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class ToastyQoLHandler : ModSystem
    {
        public static Mod ToastyQoLMod
        {
            get;
            private set;
        }

        public override void PostAddRecipes()
        {
            if (ModLoader.TryGetMod("ToastyQoL", out var result))
                ToastyQoLMod = result;
            else
                return;

            if (Main.netMode is NetmodeID.Server)
                return;

            AddBossLockInfo();
        }

        public override void OnModUnload()
        {
            ToastyQoLMod = null;
        }

        private static void AddBossLockInfo()
        {
            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedGSS, "Bereft Vassal", new List<int>()
            {
                ModContent.ItemType<CherishedSealocket>(),
                ModContent.ItemType<Myrindael>(),
                ModContent.ItemType<TheGlassmaker>(),
                ModContent.ItemType<AridBattlecry>(),
                ModContent.ItemType<WanderersShell>(),
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedProvidence, "Providence", new List<int>()
            {
                ModContent.ItemType<LunarCoin>(),
                ModContent.ItemType<Purity>(),
                ModContent.ItemType<ProfanedCrystalDye>()
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedCalamitasClone, "Calamitas", new List<int>()
            {
                ModContent.ItemType<BrimstoneCrescentStaff>()
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => NPC.downedFishron, "Duke Fishron", new List<int>()
            {
                ModContent.ItemType<Blahaj>()
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedExoMechs && DownedBossSystem._downedCalamitas, "Endgame", new List<int>()
            {
                ModContent.ItemType<HyperplaneMatrix>(),
                ModContent.ItemType<Kevin>(),
                ModContent.ItemType<StormMaidensRetribution>()
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedBossRush, "Boss Rush", new List<int>()
            {
                ModContent.ItemType<DemonicChaliceOfInfernum>()
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedAdultEidolonWyrm, "Adult Eidolon Wyrm", new List<int>()
            {
                ModContent.ItemType<EyeOfMadness>(),
                ModContent.ItemType<IllusionersReverie>(),
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedYharon, "Yharon", new List<int>()
            {
                ModContent.ItemType<Dreamtastic>()
            }, false);

            ToastyQoLMod.Call("AddNewBossLockInformation", () => NPC.downedGolemBoss, "Golem", new List<int>()
            {
                ModContent.ItemType<CallUponTheEggs>()
            }, false);
        }
    }
}
