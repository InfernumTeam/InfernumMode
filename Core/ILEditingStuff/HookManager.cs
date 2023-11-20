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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.ILEditingStuff
{
    public static partial class HookManager
    {
        internal static List<ILHook> IlHooks
        {
            get;
            set;
        } = new();

        internal static List<Hook> OnHooks
        {
            get;
            set;
        } = new();

        public static event ILContext.Manipulator ModifyPreAINPC
        {
            add => ModifyIl(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModifySetDefaultsNPC
        {
            add => ModifyIl(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModifyCheckDead
        {
            add => ModifyIl(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModifyPreDrawNPC
        {
            add => ModifyIl(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModifyPreDrawProjectile
        {
            add => ModifyIl(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModifyFindFrameNPC
        {
            add => ModifyIl(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModifyPreAIProjectile
        {
            add => ModifyIl(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ModeIndicatorUIDraw
        {
            add => ModifyIl(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator CalamityWorldPostUpdate
        {
            add => ModifyIl(typeof(WorldMiscUpdateSystem).GetMethod("PostUpdateWorld", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator CalamityPlayerModifyHitByProjectile
        {
            add => ModifyIl(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator CalamityGenNewTemple
        {
            add => ModifyIl(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SepulcherHeadModifyProjectile
        {
            add => ModifyIl(typeof(SepulcherHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SepulcherBodyModifyProjectile
        {
            add => ModifyIl(typeof(SepulcherBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SepulcherBody2ModifyProjectile
        {
            add => ModifyIl(typeof(SepulcherBodyEnergyBall).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SepulcherTailModifyProjectile
        {
            add => ModifyIl(typeof(SepulcherTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator DesertScourgeItemUseItem
        {
            add => ModifyIl(typeof(DesertMedallion).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator AresBodyCanHitPlayer
        {
            add => ModifyIl(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator YharonOnHitPlayer
        {
            add => ModifyIl(typeof(Yharon).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SCalOnHitPlayer
        {
            add => ModifyIl(typeof(SupremeCalamitas).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator NPCStatsDefineContactDamage
        {
            add => ModifyIl(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ExoMechIsPresent
        {
            add => ModifyIl(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ExoMechSelectionUIDraw
        {
            add => ModifyIl(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ExoMechDropLoot
        {
            add => ModifyIl(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator UpdateRippers
        {
            add => ModifyIl(typeof(CalamityPlayer).GetMethod("UpdateRippers", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator GetAdrenalineDamage
        {
            add => ModifyIl(typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator PlaceHellLab
        {
            add => ModifyIl(typeof(DraedonStructures).GetMethod("PlaceHellLab", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SpawnProvLootBox
        {
            add => ModifyIl(typeof(Providence).GetMethod("SpawnLootBox", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator DashMovement
        {
            add => ModifyIl(typeof(CalamityPlayer).GetMethod("ModDashMovement", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator BossRushTier
        {
            add => ModifyIl(typeof(BossRushEvent).GetMethod("get_CurrentTier", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ExoMechsSkyIsActive
        {
            add => ModifyIl(typeof(ExoMechsSky).GetMethod("get_CanSkyBeActive", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event Func<Func<Tile, bool>, Tile, bool> FargosCanDestroyTile
        {
            add => MonoModHooks.Add(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.FargoGlobalProjectile").GetMethod("OkayToDestroyTile", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge
        {
            add => ModifyIl(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.DoubleObsInstaBridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge2
        {
            add => ModifyIl(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.InstabridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator GenerateSulphSeaCheeseCaves
        {
            add => ModifyIl(typeof(SulphurousSea).GetMethod("GenerateCheeseWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator GenerateSulphSeaSpaghettiCaves
        {
            add => ModifyIl(typeof(SulphurousSea).GetMethod("GenerateSpaghettiWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event Action<Action> GenerateAbyss
        {
            add => MonoModHooks.Add(typeof(Abyss).GetMethod("PlaceAbyss", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ExoMechTileTileColor
        {
            add => ModifyIl(typeof(ExoMechsSky).GetMethod("OnTileColor", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
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
            remove => FuckYou();
        }

        public static event AbyssInBiomeHookDelegate1 IsAbyssLayer1BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer1Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event AbyssInBiomeHookDelegate2 IsAbyssLayer2BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer2Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event AbyssInBiomeHookDelegate3 IsAbyssLayer3BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer3Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event AbyssInBiomeHookDelegate4 IsAbyssLayer4BiomeActive
        {
            add => MonoModHooks.Add(typeof(AbyssLayer4Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator AbyssLayer1Color
        {
            add => ModifyIl(typeof(AbyssLayer1Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator AbyssLayer2Color
        {
            add => ModifyIl(typeof(AbyssLayer2Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator AbyssLayer3Color
        {
            add => ModifyIl(typeof(AbyssLayer3Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator AbyssLayer4Color
        {
            add => ModifyIl(typeof(AbyssLayer4Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }
        public static event ILContext.Manipulator BRSkyColor
        {
            add => ModifyIl(typeof(BossRushSky).GetMethod("get_GeneralColor", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator BRXerocEyeTexure
        {
            add => ModifyIl(typeof(BossRushSky).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator DoGSkyDraw
        {
            add => ModifyIl(typeof(DoGBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event SCalSkyDrawDelegate SCalSkyDraw
        {
            add => MonoModHooks.Add(typeof(SCalBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event CalCloneSkyDrawDelegate CalCloneSkyDraw
        {
            add => MonoModHooks.Add(typeof(CalamitasCloneBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event YharonSkyDrawDelegate YharonSkyDraw
        {
            add => MonoModHooks.Add(typeof(YharonBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator UpdateBadLifeRegen
        {
            add => ModifyIl(typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator SelectSulphuricWaterColor
        {
            add => ModifyIl(typeof(ILChanges).GetMethod("SelectSulphuricWaterColor", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator DrawCodebreakerUI
        {
            add => ModifyIl(typeof(CodebreakerUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator DisplayCodebreakerCommunicationPanel
        {
            add => ModifyIl(typeof(CodebreakerUI).GetMethod("DisplayCommunicationPanel", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator RuneOfKosCanUseItem
        {
            add => ModifyIl(typeof(RuneofKos).GetMethod("CanUseItem", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator RuneOfKosUseItem
        {
            add => ModifyIl(typeof(RuneofKos).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator PlaceForbiddenArchive
        {
            add => ModifyIl(typeof(DungeonArchive).GetMethod("PlaceArchive", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ProfanedShardCanUseItem
        {
            add => ModifyIl(typeof(ProfanedShard).GetMethod("CanUseItem", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator ProfanedShardUseItem
        {
            add => ModifyIl(typeof(ProfanedShard).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator CalGlobalNPCPostDraw
        {
            add => ModifyIl(typeof(CalamityGlobalNPC).GetMethod("PostDraw", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator AquaticScourgeSpecialOnKill
        {
            add => ModifyIl(typeof(AquaticScourgeHead).GetMethod("SpecialOnKill", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator CalPlayerProcessTriggers
        {
            add => ModifyIl(typeof(CalamityPlayer).GetMethod("ProcessTriggers", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator EternityHexAI
        {
            add => ModifyIl(typeof(EternityHex).GetMethod("AI", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        public static event ILContext.Manipulator CalamityGlobalNPCPreAI
        {
            add => ModifyIl(typeof(CalamityGlobalNPC).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => FuckYou();
        }

        private static readonly MethodInfo SetDefaultMethod = typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags);

        public delegate void Orig_SetDefaultDelegate(NPC npc, bool createModNPC);

        private static readonly MethodInfo FindFrameMethod = typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags);

        public delegate void Orig_FindFrameDelegate(NPC npc, int frameHeight);

        private static readonly MethodInfo CalPreAIMethod = typeof(CalamityGlobalNPC).GetMethod("PreAI", Utilities.UniversalBindingFlags);

        public delegate bool Orig_CalPreAIDelegate(CalamityGlobalNPC self, NPC npc);

        private static readonly MethodInfo CalGetAdrenalineDamageMethod = typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags);

        public delegate float Orig_CalGetAdrenalineDamageMethod(CalamityPlayer mp);

        private static readonly MethodInfo CalApplyRippersToDamageMethod = typeof(CalamityUtils).GetMethod("ApplyRippersToDamage", Utilities.UniversalBindingFlags);

        public delegate void Orig_CalApplyRippersToDamageMethod(CalamityPlayer mp, bool trueMelee, ref float damageMult);

        private static readonly MethodInfo CalModifyHitNPCWithProjMethod = typeof(CalamityPlayer).GetMethod("ModifyHitNPCWithProj", Utilities.UniversalBindingFlags);

        public delegate void Orig_CalModifyHitNPCWithProjMethod(CalamityPlayer self, Projectile proj, NPC target, ref NPC.HitModifiers modifiers);

        private static readonly MethodInfo CalModifyHitNPCWithItemMethod = typeof(CalamityPlayer).GetMethod("ModifyHitNPCWithItem", Utilities.UniversalBindingFlags);

        public delegate void Orig_CalModifyHitNPCWithItemMethod(CalamityPlayer self, Item item, NPC target, ref NPC.HitModifiers modifiers);

        private static readonly MethodInfo CalGlobalNPCPredrawMethod = typeof(CalamityGlobalNPC).GetMethod("PreDraw", Utilities.UniversalBindingFlags);

        public delegate bool Orig_CalGlobalNPCPredrawMethod(CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);

        public static void FuckYou()
        {

        }

        public static void ModifyIl(MethodBase method, ILContext.Manipulator edit)
        {
            IlHooks ??= new();

            var hook = new ILHook(method, edit);
            hook.Apply();
            IlHooks.Add(hook);
        }

        public static void ModifyDetour(MethodBase methodBase, Delegate method)
        {
            Hook hook = new(methodBase, method);
            hook.Apply();
            OnHooks.Add(hook);
        }
    }
}
