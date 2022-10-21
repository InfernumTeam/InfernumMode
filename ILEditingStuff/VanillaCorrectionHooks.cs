using CalamityMod;
using CalamityMod.Balancing;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Schematics;
using CalamityMod.World;
using InfernumMode.Subworlds;
using InfernumMode.Tiles.Relics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SubworldLibrary;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.Events.BossRushEvent;
using static InfernumMode.ILEditingStuff.HookManager;
using InfernumBalancingManager = InfernumMode.Balancing.BalancingChangesManager;

namespace InfernumMode.ILEditingStuff
{
    public class ReplaceGoresHook : IHookEdit
    {
        internal static int AlterGores(On.Terraria.Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.Cultist1 && Type <= GoreID.CultistBoss2)
                return Main.maxDust;

            if (InfernumMode.CanUseCustomAIs && Type == 573)
                Type = InfernumMode.Instance.Find<ModGore>("DukeFishronGore1").Type;
            if (InfernumMode.CanUseCustomAIs && Type == 574)
                Type = InfernumMode.Instance.Find<ModGore>("DukeFishronGore3").Type;
            if (InfernumMode.CanUseCustomAIs && Type == 575)
                Type = InfernumMode.Instance.Find<ModGore>("DukeFishronGore2").Type;
            if (InfernumMode.CanUseCustomAIs && Type == 576)
                Type = InfernumMode.Instance.Find<ModGore>("DukeFishronGore4").Type;

            return orig(source, Position, Velocity, Type, Scale);
        }

        public void Load() => On.Terraria.Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += AlterGores;

        public void Unload() => On.Terraria.Gore.NewGore_IEntitySource_Vector2_Vector2_int_float -= AlterGores;
    }

    public class MoveDraedonHellLabHook : IHookEdit
    {
        internal static void SlideOverHellLab(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.EmitDelegate<Action>(() =>
            {
                int tries = 0;
                string mapKey = SchematicManager.HellLabKey;
                SchematicMetaTile[,] schematic = SchematicManager.TileMaps[mapKey];

                do
                {
                    int underworldTop = Main.maxTilesY - 200;
                    int placementPositionX = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.7), (int)(Main.maxTilesX * 0.82));
                    int placementPositionY = WorldGen.genRand.Next(Main.maxTilesY - 150, Main.maxTilesY - 125);

                    Point placementPoint = new(placementPositionX, placementPositionY);
                    Vector2 schematicSize = new(schematic.GetLength(0), schematic.GetLength(1));
                    int xCheckArea = 30;
                    bool canGenerateInLocation = true;

                    // new Vector2 is used here since a lambda expression cannot capture a ref, out, or in parameter.
                    float totalTiles = (schematicSize.X + xCheckArea * 2) * schematicSize.Y;
                    for (int x = placementPoint.X - xCheckArea; x < placementPoint.X + schematicSize.X + xCheckArea; x++)
                    {
                        for (int y = placementPoint.Y; y < placementPoint.Y + schematicSize.Y; y++)
                        {
                            Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                            if (DraedonStructures.ShouldAvoidLocation(new Point(x, y), false))
                                canGenerateInLocation = false;
                        }
                    }
                    if (!canGenerateInLocation)
                    {
                        tries++;
                    }
                    else
                    {
                        bool hasPlacedMurasama = false;
                        SchematicManager.PlaceSchematic(mapKey, new Point(placementPoint.X, placementPoint.Y), SchematicAnchor.TopLeft, ref hasPlacedMurasama, new Action<Chest, int, bool>(DraedonStructures.FillHellLaboratoryChest));
                        CalamityWorld.HellLabCenter = placementPoint.ToWorldCoordinates() + new Vector2(SchematicManager.TileMaps[mapKey].GetLength(0), SchematicManager.TileMaps[mapKey].GetLength(1)) * 8f;
                        break;
                    }
                }
                while (tries <= 50000);
            });
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => PlaceHellLab += SlideOverHellLab;

        public void Unload() => PlaceHellLab -= SlideOverHellLab;
    }

    public class DrawSunkenColosseumBackgroundHook : IHookEdit
    {
        internal void ForceDrawBlack(On.Terraria.Main.orig_DrawBlack orig, Main self, bool force)
        {
            orig(self, force || SubworldSystem.IsActive<SunkenColosseum>());
        }

        internal void DrawColosseumBackground(On.Terraria.Main.orig_DrawBackground orig, Main self)
        {
            orig(self);

            if (Main.gameMenu || Main.dedServ || !SubworldSystem.IsActive<SunkenColosseum>())
                return;

            Texture2D gradient = ModContent.Request<Texture2D>("InfernumMode/Backgrounds/SunkenColosseumBGGradient").Value;
            Texture2D bgObjects = ModContent.Request<Texture2D>("InfernumMode/Backgrounds/SunkenColosseumBGObjects").Value;

            // I don't know.
            Vector2 screenOffset = Main.screenPosition + new Vector2(Main.screenWidth >> 1, Main.screenHeight >> 1);
            Vector2 vector = new Vector2(gradient.Width, gradient.Height) * 0.5f;
            float num2 = 1.08f;
            Vector2 vector2 = new(1f / num2);
            Rectangle rectangle = new(0, 0, gradient.Width, gradient.Height);
            float scale = 1.2f;
            float scaledWidth = scale * (float)rectangle.Width;
            int num11 = (int)((float)((int)(screenOffset.X * vector2.X - vector.X - (float)(Main.screenWidth >> 1))) / scaledWidth);
            vector = vector.Floor();
            int num12 = (int)Math.Ceiling((double)((float)Main.screenWidth / scaledWidth));
            int num13 = (int)(scale * ((float)(rectangle.Width - 1) / vector2.X));
            Vector2 vector3 = (new Vector2((float)((num11 - 2) * num13), (float)Main.maxTilesY * 4f) + vector - screenOffset) * vector2 + screenOffset - Main.screenPosition - vector;
            vector3 = vector3.Floor();
            while (vector3.X + scaledWidth < 0f)
            {
                num11++;
                vector3.X += scaledWidth;
            }
            vector3.Y += 250f;
            for (int i = num11 - 2; i <= num11 + num12 + 2; i++)
            {
                Main.spriteBatch.Draw(gradient, vector3 - Vector2.UnitY * 400f, rectangle, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(bgObjects, vector3, rectangle, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                vector3.X += scaledWidth;
            }
        }

        internal void ChangeDrawBlackLimit(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(x => x.MatchStloc(13)))
                return;

            c.Emit(OpCodes.Ldloc, 3);
            c.EmitDelegate<Func<float, float>>(lightThreshold =>
            {
                if (SubworldSystem.IsActive<SunkenColosseum>())
                    return 0.125f;

                return lightThreshold;
            });
            c.Emit(OpCodes.Stloc, 3);
        }

        public void Load()
        {
            On.Terraria.Main.DrawBackground += DrawColosseumBackground;
            On.Terraria.Main.DrawBlack += ForceDrawBlack;
            IL.Terraria.Main.DrawBlack += ChangeDrawBlackLimit;
        }

        public void Unload()
        {
            On.Terraria.Main.DrawBackground -= DrawColosseumBackground;
            On.Terraria.Main.DrawBlack -= ForceDrawBlack;
            IL.Terraria.Main.DrawBlack -= ChangeDrawBlackLimit;
        }
    }

    public class GetRidOfOnHitDebuffsHook : IHookEdit
    {
        public void Load()
        {
            YharonOnHitPlayer += SepulcherOnHitProjectileEffectRemovalHook.EarlyReturn;
            SCalOnHitPlayer += SepulcherOnHitProjectileEffectRemovalHook.EarlyReturn;
        }

        public void Unload()
        {
            YharonOnHitPlayer -= SepulcherOnHitProjectileEffectRemovalHook.EarlyReturn;
            SCalOnHitPlayer -= SepulcherOnHitProjectileEffectRemovalHook.EarlyReturn;
        }
    }

    public class ChangeBossRushTiersHooks : IHookEdit
    {
        internal void AdjustBossRushTiers(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.EmitDelegate(() =>
            {
                int tier2Boss = NPCID.TheDestroyer;
                int tier3Boss = NPCID.CultistBoss;
                if (InfernumMode.CanUseCustomAIs)
                {
                    tier2Boss = ModContent.NPCType<ProfanedGuardianCommander>();
                    tier3Boss = ModContent.NPCType<SlimeGodCore>();
                }

                if (BossRushStage > Bosses.FindIndex(boss => boss.EntityID == tier3Boss))
                    return 3;
                if (BossRushStage > Bosses.FindIndex(boss => boss.EntityID == tier2Boss))
                    return 2;
                return 1;
            });
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => BossRushTier += AdjustBossRushTiers;

        public void Unload() => BossRushTier -= AdjustBossRushTiers;
    }

    public class DisableExoMechsSkyInBRHook : IHookEdit
    {
        internal void DisableSky(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.EmitDelegate(() =>
            {
                int draedon = CalamityGlobalNPC.draedon;
                if (draedon == -1 || !Main.npc[draedon].active)
                    return Draedon.ExoMechIsPresent && !BossRushActive;

                if ((Main.npc[draedon]?.ModNPC<Draedon>()?.DefeatTimer ?? 0) <= 0 && !Draedon.ExoMechIsPresent)
                    return false;

                return !BossRushActive;
            });
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => ExoMechsSkyIsActive += DisableSky;

        public void Unload() => ExoMechsSkyIsActive -= DisableSky;
    }

    public class GetRidOfProvidenceLootBoxHook : IHookEdit
    {
        public void Load() => SpawnProvLootBox += SepulcherOnHitProjectileEffectRemovalHook.EarlyReturn;

        public void Unload() => SpawnProvLootBox -= SepulcherOnHitProjectileEffectRemovalHook.EarlyReturn;
    }

    public class AddWarningAboutNonExpertOnWorldSelectionHook : IHookEdit
    {
        internal static void SwapDescriptionKeys(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdstr("UI.WorldDescriptionNormal")))
                return;

            // Pop original value off.
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldstr, "Mods.InfernumMode.UI.NotExpertWarning");

            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdstr("UI.WorldDescriptionMaster")))
                return;

            // Pop original value off.
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldstr, "Mods.InfernumMode.UI.NotExpertWarning");
        }

        public void Load() => IL.Terraria.GameContent.UI.States.UIWorldCreation.AddWorldDifficultyOptions += SwapDescriptionKeys;

        public void Unload() => IL.Terraria.GameContent.UI.States.UIWorldCreation.AddWorldDifficultyOptions -= SwapDescriptionKeys;
    }

    public class ReducePlayerDashDelay : IHookEdit
    {
        internal static void ReduceDashDelays(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.After, i => i.MatchLdcI4(BalancingConstants.UniversalDashCooldown));
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, InfernumBalancingManager.DashDelay);

            c.GotoNext(MoveType.After, i => i.MatchLdcI4(BalancingConstants.UniversalShieldSlamCooldown));
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, InfernumBalancingManager.DashDelay);

            c.GotoNext(MoveType.After, i => i.MatchLdcI4(BalancingConstants.UniversalShieldBonkCooldown));
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, InfernumBalancingManager.DashDelay);
        }

        public void Load() => DashMovement += ReduceDashDelays;

        public void Unload() => DashMovement -= ReduceDashDelays;
    }

    public class AureusPlatformWalkingHook : IHookEdit
    {
        internal static bool LetAureusWalkOnPlatforms(On.Terraria.NPC.orig_Collision_DecideFallThroughPlatforms orig, NPC npc)
        {
            if (npc.type == ModContent.NPCType<AstrumAureus>())
            {
                if (Main.player[npc.target].position.Y > npc.Bottom.Y)
                    return true;
                return false;
            }
            return orig(npc);
        }

        public void Load() => On.Terraria.NPC.Collision_DecideFallThroughPlatforms += LetAureusWalkOnPlatforms;

        public void Unload() => On.Terraria.NPC.Collision_DecideFallThroughPlatforms -= LetAureusWalkOnPlatforms;
    }

    public class FishronSkyDistanceLeniancyHook : IHookEdit
    {
        internal static void AdjustFishronScreenDistanceRequirement(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchLdcR4(3000f));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_R4, 6000f);
        }

        public void Load() => IL.Terraria.GameContent.Events.ScreenDarkness.Update += AdjustFishronScreenDistanceRequirement;

        public void Unload() => IL.Terraria.GameContent.Events.ScreenDarkness.Update -= AdjustFishronScreenDistanceRequirement;
    }

    public class EyeOfCthulhuSpawnHPMinChangeHook : IHookEdit
    {
        internal static void ChangeEoCHPRequirements(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchLdcI4(200));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4, 400);
        }

        public void Load() => IL.Terraria.Main.UpdateTime_StartNight += ChangeEoCHPRequirements;

        public void Unload() => IL.Terraria.Main.UpdateTime_StartNight -= ChangeEoCHPRequirements;
    }

    public class KingSlimeSpawnHPMinChangeHook : IHookEdit
    {
        private static bool spawningKingSlimeNaturally;

        internal static void ChangeKSHPRequirements(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchCall<NPC>("SpawnOnPlayer"));
            cursor.EmitDelegate<Action>(() => spawningKingSlimeNaturally = true);
        }

        private void OptionallyDisableKSSpawn(On.Terraria.NPC.orig_SpawnOnPlayer orig, int plr, int Type)
        {
            if (spawningKingSlimeNaturally)
            {
                spawningKingSlimeNaturally = false;
                if (Main.player[plr].statLifeMax < 400)
                    return;
            }
            orig(plr, Type);
        }

        public void Load()
        {
            IL.Terraria.NPC.SpawnNPC += ChangeKSHPRequirements;
            On.Terraria.NPC.SpawnOnPlayer += OptionallyDisableKSSpawn;
        }

        public void Unload()
        {
            IL.Terraria.NPC.SpawnNPC -= ChangeKSHPRequirements;
            On.Terraria.NPC.SpawnOnPlayer -= OptionallyDisableKSSpawn;
        }
    }

    public class UseCustomShineParticlesForInfernumParticlesHook : IHookEdit
    {
        internal static void EmitFireParticles(On.Terraria.GameContent.Drawing.TileDrawing.orig_DrawTiles_EmitParticles orig, TileDrawing self, int j, int i, Tile tileCache, ushort typeCache, short tileFrameX, short tileFrameY, Color tileLight)
        {
            ModTile mt = TileLoader.GetTile(tileCache.TileType);
            if ((tileLight.R > 20 || tileLight.B > 20 || tileLight.G > 20) && Main.rand.NextBool(12) && mt is not null and BaseInfernumBossRelic)
            {
                Dust fire = Dust.NewDustDirect(new Vector2(i * 16, j * 16), 16, 16, Main.rand.NextBool() ? 267 : 6, 0f, 0f, 254, Color.White, 1.4f);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.5f);
                fire.color = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.7f));
                fire.noGravity = true;
            }

            // I LOVE RANDOM ERRORS IN VANILLA METHODS THAT DISRUPT MY GODDAMN DEBUGGING ENVIRONMENT.
            // It's so FUN!
            try
            {
                orig(self, i, j, tileCache, typeCache, tileFrameX, tileFrameY, tileLight);
            }
            catch (IndexOutOfRangeException) { }
        }

        public void Load() => On.Terraria.GameContent.Drawing.TileDrawing.DrawTiles_EmitParticles += EmitFireParticles;

        public void Unload() => On.Terraria.GameContent.Drawing.TileDrawing.DrawTiles_EmitParticles -= EmitFireParticles;
    }

    public class LessenDesertTileRequirementsHook : IHookEdit
    {
        internal static void MakeDesertRequirementsMoreLenient(On.Terraria.Player.orig_UpdateBiomes orig, Player self)
        {
            orig(self);
            self.ZoneDesert = Main.SceneMetrics.SandTileCount > 300;
        }

        public void Load() => On.Terraria.Player.UpdateBiomes += MakeDesertRequirementsMoreLenient;

        public void Unload() => On.Terraria.Player.UpdateBiomes -= MakeDesertRequirementsMoreLenient;
    }

    public class SepulcherOnHitProjectileEffectRemovalHook : IHookEdit
    {
        internal static void EarlyReturn(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load()
        {
            SepulcherHeadModifyProjectile += EarlyReturn;
            SepulcherBodyModifyProjectile += EarlyReturn;
            SepulcherBody2ModifyProjectile += EarlyReturn;
            SepulcherTailModifyProjectile += EarlyReturn;
        }

        public void Unload()
        {
            SepulcherHeadModifyProjectile -= EarlyReturn;
            SepulcherBodyModifyProjectile -= EarlyReturn;
            SepulcherBody2ModifyProjectile -= EarlyReturn;
            SepulcherTailModifyProjectile -= EarlyReturn;
        }
    }

    public class GetRidOfDesertNuisancesHook : IHookEdit
    {
        internal static void GetRidOfDesertNuisances(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<Player>>(player =>
            {
                int scourgeID = ModContent.NPCType<DesertScourgeHead>();
                if (NPC.AnyNPCs(scourgeID))
                    return;

                SoundEngine.PlaySound(SoundID.Roar, player.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(player.whoAmI, scourgeID);
                else
                    NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, scourgeID);

                // Summon nuisances if not in Infernum mode.
                if (CalamityWorld.revenge && !InfernumMode.CanUseCustomAIs)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());
                    else
                        NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());
                    else
                        NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());
                }
            });
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => DesertScourgeItemUseItem += GetRidOfDesertNuisances;

        public void Unload() => DesertScourgeItemUseItem -= GetRidOfDesertNuisances;
    }

    public class LetAresHitPlayersHook : IHookEdit
    {
        internal static void LetAresHitPlayer(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => AresBodyCanHitPlayer += LetAresHitPlayer;

        public void Unload() => AresBodyCanHitPlayer -= LetAresHitPlayer;
    }
}