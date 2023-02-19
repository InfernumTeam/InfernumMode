using CalamityMod;
using CalamityMod.BiomeManagers;
using CalamityMod.CalPlayer;
using CalamityMod.Events;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
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

namespace InfernumMode.ILEditingStuff
{
    public static partial class HookManager
    {
        public static event ILContext.Manipulator ModifyPreAINPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifySetDefaultsNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyCheckDead
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreDrawNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreDrawProjectile
        {
            add => HookEndpointManager.Modify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyFindFrameNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreAIProjectile
        {
            add => HookEndpointManager.Modify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyTextUtility
        {
            add => HookEndpointManager.Modify(typeof(CalamityUtils).GetMethod("DisplayLocalizedText", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityUtils).GetMethod("DisplayLocalizedText", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModeIndicatorUIDraw
        {
            add => HookEndpointManager.Modify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityWorldPostUpdate
        {
            add => HookEndpointManager.Modify(typeof(WorldMiscUpdateSystem).GetMethod("PostUpdateWorld", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(WorldMiscUpdateSystem).GetMethod("PostUpdateWorld", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityPlayerModifyHitByProjectile
        {
            add => HookEndpointManager.Modify(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityNPCLifeRegen
        {
            add => HookEndpointManager.Modify(typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityGenNewTemple
        {
            add => HookEndpointManager.Modify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherHeadModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SepulcherHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SepulcherHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherBodyModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SepulcherBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SepulcherBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherBody2ModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SepulcherBodyEnergyBall).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SepulcherBodyEnergyBall).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherTailModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SepulcherTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SepulcherTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator DesertScourgeItemUseItem
        {
            add => HookEndpointManager.Modify(typeof(DesertMedallion).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(DesertMedallion).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AresBodyCanHitPlayer
        {
            add => HookEndpointManager.Modify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator YharonOnHitPlayer
        {
            add => HookEndpointManager.Modify(typeof(Yharon).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(Yharon).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SCalOnHitPlayer
        {
            add => HookEndpointManager.Modify(typeof(SupremeCalamitas).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SupremeCalamitas).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator NPCStatsDefineContactDamage
        {
            add => HookEndpointManager.Modify(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechIsPresent
        {
            add => HookEndpointManager.Modify(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechSelectionUIDraw
        {
            add => HookEndpointManager.Modify(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechDropLoot
        {
            add => HookEndpointManager.Modify(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator UpdateRippers
        {
            add => HookEndpointManager.Modify(typeof(CalamityPlayer).GetMethod("UpdateRippers", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityPlayer).GetMethod("UpdateRippers", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator GetAdrenalineDamage
        {
            add => HookEndpointManager.Modify(typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator PlaceHellLab
        {
            add => HookEndpointManager.Modify(typeof(DraedonStructures).GetMethod("PlaceHellLab", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(DraedonStructures).GetMethod("PlaceHellLab", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SpawnProvLootBox
        {
            add => HookEndpointManager.Modify(typeof(Providence).GetMethod("SpawnLootBox", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(Providence).GetMethod("SpawnLootBox", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator DashMovement
        {
            add => HookEndpointManager.Modify(typeof(CalamityPlayer).GetMethod("ModDashMovement", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityPlayer).GetMethod("ModDashMovement", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator BossRushTier
        {
            add => HookEndpointManager.Modify(typeof(BossRushEvent).GetMethod("get_CurrentTier", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(BossRushEvent).GetMethod("get_CurrentTier", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechsSkyIsActive
        {
            add => HookEndpointManager.Modify(typeof(ExoMechsSky).GetMethod("get_CanSkyBeActive", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ExoMechsSky).GetMethod("get_CanSkyBeActive", Utilities.UniversalBindingFlags), value);
        }

        public static event Func<Func<Tile, bool>, Tile, bool> FargosCanDestroyTile
        {
            add => HookEndpointManager.Add(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.FargoGlobalProjectile").GetMethod("OkayToDestroyTile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.FargoGlobalProjectile").GetMethod("OkayToDestroyTile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge
        {
            add => HookEndpointManager.Modify(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.DoubleObsInstaBridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.DoubleObsInstaBridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge2
        {
            add => HookEndpointManager.Modify(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.InstabridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.InstabridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator GenerateSulphSeaCheeseCaves
        {
            add => HookEndpointManager.Modify(typeof(SulphurousSea).GetMethod("GenerateCheeseWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SulphurousSea).GetMethod("GenerateCheeseWaterCaves", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator GenerateSulphSeaSpaghettiCaves
        {
            add => HookEndpointManager.Modify(typeof(SulphurousSea).GetMethod("GenerateSpaghettiWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SulphurousSea).GetMethod("GenerateSpaghettiWaterCaves", Utilities.UniversalBindingFlags), value);
        }

        public static event Action<Action> GenerateAbyss
        {
            add => HookEndpointManager.Add(typeof(Abyss).GetMethod("PlaceAbyss", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(Abyss).GetMethod("PlaceAbyss", Utilities.UniversalBindingFlags), value);
        }
        
        public static event ILContext.Manipulator ExoMechTileTileColor
        {
            add => HookEndpointManager.Modify(typeof(ExoMechsSky).GetMethod("OnTileColor", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ExoMechsSky).GetMethod("OnTileColor", Utilities.UniversalBindingFlags), value);
        }

        public delegate bool AbyssRequirementDelegate(Player player, out int playerYTileCoords);

        public delegate bool AbyssRequirementHookDelegate(AbyssRequirementDelegate orig, Player player, out int playerYTileCoords);

        public delegate bool AbyssInBiomeHookDelegate1(Func<AbyssLayer1Biome, Player, bool> orig, AbyssLayer1Biome self, Player player);

        public delegate bool AbyssInBiomeHookDelegate2(Func<AbyssLayer2Biome, Player, bool> orig, AbyssLayer2Biome self, Player player);

        public delegate bool AbyssInBiomeHookDelegate3(Func<AbyssLayer3Biome, Player, bool> orig, AbyssLayer3Biome self, Player player);

        public delegate bool AbyssInBiomeHookDelegate4(Func<AbyssLayer4Biome, Player, bool> orig, AbyssLayer4Biome self, Player player);

        public delegate void SCalSkyDrawDelegate(Action<SCalBackgroundScene, Player, bool> orig, SCalBackgroundScene instance, Player player, bool isActive);

        public static event AbyssRequirementHookDelegate MeetsBaseAbyssRequirement
        {
            add => HookEndpointManager.Add(typeof(AbyssLayer1Biome).GetMethod("MeetsBaseAbyssRequirement", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(AbyssLayer1Biome).GetMethod("MeetsBaseAbyssRequirement", Utilities.UniversalBindingFlags), value);
        }

        public static event AbyssInBiomeHookDelegate1 IsAbyssLayer1BiomeActive
        {
            add => HookEndpointManager.Add(typeof(AbyssLayer1Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(AbyssLayer1Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
        }

        public static event AbyssInBiomeHookDelegate2 IsAbyssLayer2BiomeActive
        {
            add => HookEndpointManager.Add(typeof(AbyssLayer2Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(AbyssLayer2Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
        }

        public static event AbyssInBiomeHookDelegate3 IsAbyssLayer3BiomeActive
        {
            add => HookEndpointManager.Add(typeof(AbyssLayer3Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(AbyssLayer3Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
        }

        public static event AbyssInBiomeHookDelegate4 IsAbyssLayer4BiomeActive
        {
            add => HookEndpointManager.Add(typeof(AbyssLayer4Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(AbyssLayer4Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AbyssLayer1Color
        {
            add => HookEndpointManager.Modify(typeof(AbyssLayer1Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AbyssLayer1Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AbyssLayer2Color
        {
            add => HookEndpointManager.Modify(typeof(AbyssLayer2Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AbyssLayer2Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AbyssLayer3Color
        {
            add => HookEndpointManager.Modify(typeof(AbyssLayer3Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AbyssLayer3Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AbyssLayer4Color
        {
            add => HookEndpointManager.Modify(typeof(AbyssLayer4Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AbyssLayer4Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
        }
        public static event ILContext.Manipulator BRSkyColor
        {
            add => HookEndpointManager.Modify(typeof(BossRushSky).GetMethod("get_GeneralColor", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(BossRushSky).GetMethod("get_GeneralColor", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator BRXerocEyeTexure
        {
            add => HookEndpointManager.Modify(typeof(BossRushSky).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(BossRushSky).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator DoGSkyDraw
        {
            add => HookEndpointManager.Modify(typeof(DoGBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(DoGBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
        }

        public static event SCalSkyDrawDelegate SCalSkyDraw
        {
            add => HookEndpointManager.Add(typeof(SCalBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Remove(typeof(SCalBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator UpdateBadLifeRegen
        {
            add => HookEndpointManager.Modify(typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags), value);
        }
    }
}