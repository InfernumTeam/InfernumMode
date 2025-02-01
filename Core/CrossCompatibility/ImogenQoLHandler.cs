using System.Collections.Generic;
using CalamityMod;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Items.Dyes;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Items.Pets;
using InfernumMode.Content.Items.Placeables;
using InfernumMode.Content.Items.Weapons.Magic;
using InfernumMode.Content.Items.Weapons.Melee;
using InfernumMode.Content.Items.Weapons.Ranged;
using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class ImogenQoLHandler : ModSystem
    {
        public static Mod ImogenQoLMod
        {
            get;
            private set;
        }

        // Deus has a weight of 17.5, so have a value just before that.
        public const float BereftVassalWeight = 17.33f;

        public override void PostAddRecipes()
        {
            if (!ModLoader.TryGetMod("ToastyQoL", out var result))
                return;

            ImogenQoLMod = result;

            if (Main.netMode is NetmodeID.Server)
                return;

            AddBossLockInfo();

            // Register Bereft Vassal in the boss status toggle UI.
            ImogenQoLMod.Call("AddBossToggle", "InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassal_Head_Boss", "Bereft Vassal",
                typeof(WorldSaveSystem).GetField("downedBereftVassal", Utilities.UniversalBindingFlags), BereftVassalWeight, 1f);
        }

        public override void OnModUnload() => ImogenQoLMod = null;

        private static void AddBossLockInfo()
        {
            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedGSS, "Bereft Vassal", new List<int>()
            {
                ModContent.ItemType<CherishedSealocket>(),
                ModContent.ItemType<Myrindael>(),
                ModContent.ItemType<TheGlassmaker>(),
                ModContent.ItemType<AridBattlecry>(),
                ModContent.ItemType<WanderersShell>(),
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedProvidence, "Providence", new List<int>()
            {
                ModContent.ItemType<LunarCoin>(),
                ModContent.ItemType<Purity>(),
                ModContent.ItemType<ProfanedCrystalDye>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedCalamitasClone, "Calamitas", new List<int>()
            {
                ModContent.ItemType<BrimstoneCrescentStaff>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => NPC.downedFishron, "Duke Fishron", new List<int>()
            {
                ModContent.ItemType<Blahaj>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedExoMechs && DownedBossSystem._downedCalamitas, "Endgame", new List<int>()
            {
                ModContent.ItemType<HyperplaneMatrix>(),
                ModContent.ItemType<Kevin>(),
                ModContent.ItemType<StormMaidensRetribution>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedBossRush, "Boss Rush", new List<int>()
            {
                ModContent.ItemType<DemonicChaliceOfInfernum>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem.downedPrimordialWyrm, "Adult Eidolon Wyrm", new List<int>()
            {
                ModContent.ItemType<EyeOfMadness>(),
                ModContent.ItemType<IllusionersReverie>(),
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedYharon, "Yharon", new List<int>()
            {
                ModContent.ItemType<Dreamtastic>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => NPC.downedGolemBoss, "Golem", new List<int>()
            {
                ModContent.ItemType<CallUponTheEggs>()
            }, false);

            ImogenQoLMod.Call("AddNewBossLockInformation", () => DownedBossSystem._downedGuardians, "Profaned Guardians", new List<int>()
            {
                ModContent.ItemType<Punctus>()
            }, false);
        }
    }
}
