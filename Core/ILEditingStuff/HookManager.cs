using CalamityMod;
using CalamityMod.BiomeManagers;
using CalamityMod.CalPlayer;
using CalamityMod.Events;
using CalamityMod.ILEditing;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Projectiles.Magic;
using CalamityMod.Skies;
using CalamityMod.Systems;
using CalamityMod.UI.DraedonSummoning;
using CalamityMod.UI.ModeIndicator;
using CalamityMod.World;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.ILEditingStuff
{
    public static partial class HookManager
    {
        public static event ILContext.Manipulator ModifyPreAINPC
        {
            add => MonoModHooks.Modify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModifySetDefaultsNPC
        {
            add => MonoModHooks.Modify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModifyCheckDead
        {
            add => MonoModHooks.Modify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModifyPreDrawNPC
        {
            add => MonoModHooks.Modify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModifyPreDrawProjectile
        {
            add => MonoModHooks.Modify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModifyFindFrameNPC
        {
            add => MonoModHooks.Modify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModifyPreAIProjectile
        {
            add => MonoModHooks.Modify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ModeIndicatorUIDraw
        {
            add => MonoModHooks.Modify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator CalamityWorldPostUpdate
        {
            add => MonoModHooks.Modify(typeof(WorldMiscUpdateSystem).GetMethod("PostUpdateWorld", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator CalamityPlayerModifyHitByProjectile
        {
            add => MonoModHooks.Modify(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator CalamityGenNewTemple
        {
            add => MonoModHooks.Modify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SepulcherHeadModifyProjectile
        {
            add => MonoModHooks.Modify(typeof(SepulcherHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SepulcherBodyModifyProjectile
        {
            add => MonoModHooks.Modify(typeof(SepulcherBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SepulcherBody2ModifyProjectile
        {
            add => MonoModHooks.Modify(typeof(SepulcherBodyEnergyBall).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SepulcherTailModifyProjectile
        {
            add => MonoModHooks.Modify(typeof(SepulcherTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator DesertScourgeItemUseItem
        {
            add => MonoModHooks.Modify(typeof(DesertMedallion).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator AresBodyCanHitPlayer
        {
            add => MonoModHooks.Modify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator YharonOnHitPlayer
        {
            add => MonoModHooks.Modify(typeof(Yharon).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SCalOnHitPlayer
        {
            add => MonoModHooks.Modify(typeof(SupremeCalamitas).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator NPCStatsDefineContactDamage
        {
            add => MonoModHooks.Modify(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ExoMechIsPresent
        {
            add => MonoModHooks.Modify(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ExoMechSelectionUIDraw
        {
            add => MonoModHooks.Modify(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ExoMechDropLoot
        {
            add => MonoModHooks.Modify(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator UpdateRippers
        {
            add => MonoModHooks.Modify(typeof(CalamityPlayer).GetMethod("UpdateRippers", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator GetAdrenalineDamage
        {
            add => MonoModHooks.Modify(typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator PlaceHellLab
        {
            add => MonoModHooks.Modify(typeof(DraedonStructures).GetMethod("PlaceHellLab", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SpawnProvLootBox
        {
            add => MonoModHooks.Modify(typeof(Providence).GetMethod("SpawnLootBox", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator DashMovement
        {
            add => MonoModHooks.Modify(typeof(CalamityPlayer).GetMethod("ModDashMovement", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator BossRushTier
        {
            add => MonoModHooks.Modify(typeof(BossRushEvent).GetMethod("get_CurrentTier", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ExoMechsSkyIsActive
        {
            add => MonoModHooks.Modify(typeof(ExoMechsSky).GetMethod("get_CanSkyBeActive", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event Func<Func<Tile, bool>, Tile, bool> FargosCanDestroyTile
        {
            add => MonoModHooks.Add(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.FargoGlobalProjectile").GetMethod("OkayToDestroyTile", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge
        {
            add => MonoModHooks.Modify(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.DoubleObsInstaBridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge2
        {
            add => MonoModHooks.Modify(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.InstabridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator GenerateSulphSeaCheeseCaves
        {
            add => MonoModHooks.Modify(typeof(SulphurousSea).GetMethod("GenerateCheeseWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator GenerateSulphSeaSpaghettiCaves
        {
            add => MonoModHooks.Modify(typeof(SulphurousSea).GetMethod("GenerateSpaghettiWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event Action<Action> GenerateAbyss
        {
            add => MonoModHooks.Add(typeof(Abyss).GetMethod("PlaceAbyss", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ExoMechTileTileColor
        {
            add => MonoModHooks.Modify(typeof(ExoMechsSky).GetMethod("OnTileColor", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public delegate bool AbyssRequirementDelegate(Player player, out int playerYTileCoords);

        public delegate bool AbyssRequirementHookDelegate(AbyssRequirementDelegate orig, Player player, out int playerYTileCoords);

        public delegate bool AbyssInBiomeHookDelegate1(Func<AbyssLayer1Biome, Player, bool> orig, AbyssLayer1Biome self, Player player);

        public delegate bool AbyssInBiomeHookDelegate2(Func<AbyssLayer2Biome, Player, bool> orig, AbyssLayer2Biome self, Player player);

        public delegate bool AbyssInBiomeHookDelegate3(Func<AbyssLayer3Biome, Player, bool> orig, AbyssLayer3Biome self, Player player);

        public delegate bool AbyssInBiomeHookDelegate4(Func<AbyssLayer4Biome, Player, bool> orig, AbyssLayer4Biome self, Player player);

        public delegate void SCalSkyDrawDelegate(Action<SCalBackgroundScene, Player, bool> orig, SCalBackgroundScene instance, Player player, bool isActive);

        public delegate void CalCloneSkyDrawDelegate(Action<CalamitasCloneBackgroundScene, Player, bool> orig, CalamitasCloneBackgroundScene instance, Player player, bool isActive);

        public delegate void YharonSkyDrawDelegate(Action<YharonBackgroundScene, Player, bool> orig, YharonBackgroundScene instance, Player player, bool isActive);

        public static event AbyssRequirementHookDelegate MeetsBaseAbyssRequirement
        {
            add => MonoModHooks.Add(typeof(AbyssLayer1Biome).GetMethod("MeetsBaseAbyssRequirement", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event AbyssInBiomeHookDelegate1 IsAbyssLayer1BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer1Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event AbyssInBiomeHookDelegate2 IsAbyssLayer2BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer2Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event AbyssInBiomeHookDelegate3 IsAbyssLayer3BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer3Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event AbyssInBiomeHookDelegate4 IsAbyssLayer4BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer4Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator AbyssLayer1Color
        {
            add => MonoModHooks.Modify(typeof(AbyssLayer1Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator AbyssLayer2Color
        {
            add => MonoModHooks.Modify(typeof(AbyssLayer2Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator AbyssLayer3Color
        {
            add => MonoModHooks.Modify(typeof(AbyssLayer3Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator AbyssLayer4Color
        {
            add => MonoModHooks.Modify(typeof(AbyssLayer4Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }
        public static event ILContext.Manipulator BRSkyColor
        {
            add => MonoModHooks.Modify(typeof(BossRushSky).GetMethod("get_GeneralColor", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator BRXerocEyeTexure
        {
            add => MonoModHooks.Modify(typeof(BossRushSky).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator DoGSkyDraw
        {
            add => MonoModHooks.Modify(typeof(DoGBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event SCalSkyDrawDelegate SCalSkyDraw
        {
            add => MonoModHooks.Add(typeof(SCalBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event CalCloneSkyDrawDelegate CalCloneSkyDraw
        {
            add => MonoModHooks.Add(typeof(CalamitasCloneBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event YharonSkyDrawDelegate YharonSkyDraw
        {
            add => MonoModHooks.Add(typeof(YharonBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator UpdateBadLifeRegen
        {
            add => MonoModHooks.Modify(typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator SelectSulphuricWaterColor
        {
            add => MonoModHooks.Modify(typeof(ILChanges).GetMethod("SelectSulphuricWaterColor", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator DrawCodebreakerUI
        {
            add => MonoModHooks.Modify(typeof(CodebreakerUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator DisplayCodebreakerCommunicationPanel
        {
            add => MonoModHooks.Modify(typeof(CodebreakerUI).GetMethod("DisplayCommunicationPanel", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator RuneOfKosCanUseItem
        {
            add => MonoModHooks.Modify(typeof(RuneofKos).GetMethod("CanUseItem", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator RuneOfKosUseItem
        {
            add => MonoModHooks.Modify(typeof(RuneofKos).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator PlaceForbiddenArchive
        {
            add => MonoModHooks.Modify(typeof(DungeonArchive).GetMethod("PlaceArchive", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator ProfanedShardUseItem
        {
            add => MonoModHooks.Modify(typeof(ProfanedShard).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator CalGlobalNPCPostDraw
        {
            add => MonoModHooks.Modify(typeof(CalamityGlobalNPC).GetMethod("PostDraw", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator AquaticScourgeSpecialOnKill
        {
            add => MonoModHooks.Modify(typeof(AquaticScourgeHead).GetMethod("SpecialOnKill", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator CalPlayerProcessTriggers
        {
            add => MonoModHooks.Modify(typeof(CalamityPlayer).GetMethod("ProcessTriggers", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static event ILContext.Manipulator EternityHexAI
        {
            add => MonoModHooks.Modify(typeof(EternityHex).GetMethod("AI", Utilities.UniversalBindingFlags), value);
            remove => Why();
        }

        public static void Why()
        {

        }
    }
}
