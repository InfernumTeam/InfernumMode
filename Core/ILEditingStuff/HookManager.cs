using System;
using System.Reflection;
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
using Luminance.Core.Hooking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.ILEditingStuff
{
    public static partial class HookManager
    {
        public static event ILContext.Manipulator ModifyPreAINPC
        {
            add => HookHelper.ModifyMethodWithIL(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModifySetDefaultsNPC
        {
            add => HookHelper.ModifyMethodWithIL(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModifyCheckDead
        {
            add => HookHelper.ModifyMethodWithIL(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModifyPreDrawNPC
        {
            add => HookHelper.ModifyMethodWithIL(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModifyPreDrawProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModifyFindFrameNPC
        {
            add => HookHelper.ModifyMethodWithIL(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModifyPreAIProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ModeIndicatorUIDraw
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator CalamityWorldPostUpdate
        {
            add => HookHelper.ModifyMethodWithIL(typeof(WorldMiscUpdateSystem).GetMethod("PostUpdateWorld", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator CalamityPlayerModifyHitByProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator CalamityGenNewTemple
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SepulcherHeadModifyProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SepulcherHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SepulcherBodyModifyProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SepulcherBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SepulcherBody2ModifyProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SepulcherBodyEnergyBall).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SepulcherTailModifyProjectile
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SepulcherTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator DesertScourgeItemUseItem
        {
            add => HookHelper.ModifyMethodWithIL(typeof(DesertMedallion).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator AresBodyCanHitPlayer
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator YharonOnHitPlayer
        {
            add => HookHelper.ModifyMethodWithIL(typeof(Yharon).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SCalOnHitPlayer
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SupremeCalamitas).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator NPCStatsDefineContactDamage
        {
            add => HookHelper.ModifyMethodWithIL(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ExoMechIsPresent
        {
            add => HookHelper.ModifyMethodWithIL(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ExoMechSelectionUIDraw
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ExoMechDropLoot
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator UpdateRippers
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityPlayer).GetMethod("UpdateRippers", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator GetAdrenalineDamage
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator PlaceHellLab
        {
            add => HookHelper.ModifyMethodWithIL(typeof(DraedonStructures).GetMethod("PlaceHellLab", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SpawnProvLootBox
        {
            add => HookHelper.ModifyMethodWithIL(typeof(Providence).GetMethod("SpawnLootBox", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator DashMovement
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityPlayer).GetMethod("ModDashMovement", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator BossRushTier
        {
            add => HookHelper.ModifyMethodWithIL(typeof(BossRushEvent).GetMethod("get_CurrentTier", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ExoMechsSkyIsActive
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ExoMechsSky).GetMethod("get_CanSkyBeActive", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event Func<Func<Tile, bool>, Tile, bool> FargosCanDestroyTile
        {
            add => MonoModHooks.Add(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.FargoGlobalProjectile").GetMethod("OkayToDestroyTile", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge
        {
            add => HookHelper.ModifyMethodWithIL(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.DoubleObsInstaBridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator FargosCanDestroyTileWithInstabridge2
        {
            add => HookHelper.ModifyMethodWithIL(InfernumMode.FargosMutantMod.Code.GetType("Fargowiltas.Projectiles.Explosives.InstabridgeProj").GetMethod("Kill", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator GenerateSulphSeaCheeseCaves
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SulphurousSea).GetMethod("GenerateCheeseWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator GenerateSulphSeaSpaghettiCaves
        {
            add => HookHelper.ModifyMethodWithIL(typeof(SulphurousSea).GetMethod("GenerateSpaghettiWaterCaves", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ExoMechTileTileColor
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ExoMechsSky).GetMethod("OnTileColor", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
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

        public static event ILContext.Manipulator AbyssLayer1Color
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AbyssLayer1Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator AbyssLayer2Color
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AbyssLayer2Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator AbyssLayer3Color
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AbyssLayer3Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator AbyssLayer4Color
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AbyssLayer4Biome).GetMethod("get_WaterStyle", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }
        public static event ILContext.Manipulator BRSkyColor
        {
            add => HookHelper.ModifyMethodWithIL(typeof(BossRushSky).GetMethod("get_GeneralColor", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator BRXerocEyeTexure
        {
            add => HookHelper.ModifyMethodWithIL(typeof(BossRushSky).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator DoGSkyDraw
        {
            add => HookHelper.ModifyMethodWithIL(typeof(DoGBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator UpdateBadLifeRegen
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator SelectSulphuricWaterColor
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ILChanges).GetMethod("SelectSulphuricWaterColor", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator DrawCodebreakerUI
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CodebreakerUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator DisplayCodebreakerCommunicationPanel
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CodebreakerUI).GetMethod("DisplayCommunicationPanel", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator RuneOfKosCanUseItem
        {
            add => HookHelper.ModifyMethodWithIL(typeof(RuneofKos).GetMethod("CanUseItem", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator RuneOfKosUseItem
        {
            add => HookHelper.ModifyMethodWithIL(typeof(RuneofKos).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator PlaceForbiddenArchive
        {
            add => HookHelper.ModifyMethodWithIL(typeof(DungeonArchive).GetMethod("PlaceArchive", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ProfanedShardCanUseItem
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ProfanedShard).GetMethod("CanUseItem", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator ProfanedShardUseItem
        {
            add => HookHelper.ModifyMethodWithIL(typeof(ProfanedShard).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator CalGlobalNPCPostDraw
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityGlobalNPC).GetMethod("PostDraw", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator AquaticScourgeSpecialOnKill
        {
            add => HookHelper.ModifyMethodWithIL(typeof(AquaticScourgeHead).GetMethod("SpecialOnKill", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator CalPlayerProcessTriggers
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityPlayer).GetMethod("ProcessTriggers", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator EternityHexAI
        {
            add => HookHelper.ModifyMethodWithIL(typeof(EternityHex).GetMethod("AI", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        public static event ILContext.Manipulator CalamityGlobalNPCPreAI
        {
            add => HookHelper.ModifyMethodWithIL(typeof(CalamityGlobalNPC).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookHelper.ILEventRemove();
        }

        internal static MethodInfo SetDefaultMethod => typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags);

        public delegate void Orig_SetDefaultDelegate(NPC npc, bool createModNPC);

        internal static MethodInfo FindFrameMethod => typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags);

        public delegate void Orig_FindFrameDelegate(NPC npc, int frameHeight);

        internal static MethodInfo CalPreAIMethod => typeof(CalamityGlobalNPC).GetMethod("PreAI", Utilities.UniversalBindingFlags);

        public delegate bool Orig_CalPreAIDelegate(CalamityGlobalNPC self, NPC npc);

        internal static MethodInfo CalGetAdrenalineDamageMethod => typeof(CalamityUtils).GetMethod("GetAdrenalineDamage", Utilities.UniversalBindingFlags);

        public delegate float Orig_CalGetAdrenalineDamageMethod(CalamityPlayer mp);

        internal static MethodInfo CalApplyRippersToDamageMethod => typeof(CalamityUtils).GetMethod("ApplyRippersToDamage", Utilities.UniversalBindingFlags);

        public delegate void Orig_CalApplyRippersToDamageMethod(CalamityPlayer mp, bool trueMelee, ref float damageMult);

        internal static MethodInfo CalModifyHitNPCWithProjMethod => typeof(CalamityPlayer).GetMethod("ModifyHitNPCWithProj", Utilities.UniversalBindingFlags);

        public delegate void Orig_CalModifyHitNPCWithProjMethod(CalamityPlayer self, Projectile proj, NPC target, ref NPC.HitModifiers modifiers);

        internal static MethodInfo CalModifyHitNPCWithItemMethod => typeof(CalamityPlayer).GetMethod("ModifyHitNPCWithItem", Utilities.UniversalBindingFlags);

        public delegate void Orig_CalModifyHitNPCWithItemMethod(CalamityPlayer self, Item item, NPC target, ref NPC.HitModifiers modifiers);

        internal static MethodInfo CalGlobalNPCPredrawMethod => typeof(CalamityGlobalNPC).GetMethod("PreDraw", Utilities.UniversalBindingFlags);

        public delegate bool Orig_CalGlobalNPCPredrawMethod(CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);
    }
}
