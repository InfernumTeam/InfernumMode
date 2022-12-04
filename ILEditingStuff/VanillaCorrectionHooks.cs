using CalamityMod;
using CalamityMod.Balancing;
using CalamityMod.BiomeManagers;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Schematics;
using CalamityMod.Walls;
using CalamityMod.World;
using InfernumMode.Subworlds;
using InfernumMode.Systems;
using InfernumMode.Tiles.Relics;
using InfernumMode.WorldGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SubworldLibrary;
using System;
using System.Reflection;
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
            // Do not spawn gores on the server.
            if (Main.netMode == NetmodeID.Server || Main.gamePaused) 
                return 600;

            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.Cultist1 && Type <= GoreID.CultistBoss2)
                return Main.maxDust;
            
            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.HallowBoss1 && Type <= GoreID.HallowBoss7)
                return Main.maxDust;

            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.DeerclopsHead && Type <= GoreID.DeerclopsLeg)
                return Main.maxDust;

            if (InfernumMode.CanUseCustomAIs)
            {
                for (int i = 2; i <= 4; i++)
                {
                    if (Type == InfernumMode.CalamityMod.Find<ModGore>("Hive" + i).Type || Type == InfernumMode.CalamityMod.Find<ModGore>("Hive").Type)
                        return Main.maxDust;
                }
            }

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

    public class DrawLostColosseumBackgroundHook : IHookEdit
    {
        internal void ForceDrawBlack(On.Terraria.Main.orig_DrawBlack orig, Main self, bool force)
        {
            orig(self, force || SubworldSystem.IsActive<LostColosseum>());
        }

        internal void DrawColosseumBackground(On.Terraria.Main.orig_DrawBackground orig, Main self)
        {
            if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.Calamity().ZoneAbyss)
                return;

            orig(self);

            if (Main.gameMenu || Main.dedServ || !SubworldSystem.IsActive<LostColosseum>())
                return;

            Texture2D gradient = ModContent.Request<Texture2D>("InfernumMode/Backgrounds/LostColosseumBGGradient").Value;
            Texture2D bgObjects = ModContent.Request<Texture2D>("InfernumMode/Backgrounds/LostColosseumBGObjects").Value;

            // I don't know.
            // I'm sorry.
            Vector2 screenOffset = Main.screenPosition + new Vector2(Main.screenWidth >> 1, Main.screenHeight >> 1);
            Vector2 center = new Vector2(gradient.Width, gradient.Height) * 0.5f;
            Vector2 whyRedigit = new(0.92592f);
            float scale = 1.2f;
            float scaledWidth = scale * gradient.Width;
            int range = (int)(screenOffset.X * whyRedigit.X - center.X - (Main.screenWidth >> 1) / scaledWidth);
            center = center.Floor();
            int aspectRatio = (int)Math.Ceiling(Main.screenWidth / scaledWidth);
            int whyRedigit2 = (int)(scale * (gradient.Width - 1) / whyRedigit.X);
            Vector2 drawPosition = (new Vector2((range - 2) / whyRedigit2, Main.maxTilesY * 4f) + center - screenOffset) * whyRedigit + screenOffset - Main.screenPosition - center;
            drawPosition = drawPosition.Floor();

            while (drawPosition.X + scaledWidth < 0f)
            {
                range++;
                drawPosition.X += scaledWidth;
            }
            drawPosition.Y += 250f;
            for (int i = range - 2; i <= range + aspectRatio + 2; i++)
            {
                Main.spriteBatch.Draw(gradient, drawPosition - Vector2.UnitY * 400f, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(bgObjects, drawPosition, gradient.Frame(), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                drawPosition.X += scaledWidth;
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
                if (SubworldSystem.IsActive<LostColosseum>())
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

    public class ChangeBossRushTiersHook : IHookEdit
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

    public class ChangeExoMechBackgroundColorHook : IHookEdit
    {
        internal void MakeExoMechBgMoreCyan(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(MoveType.Before, i => i.MatchRet());

            cursor.EmitDelegate<Func<Color, Color>>(originalColor =>
            {
                if (!InfernumMode.CanUseCustomAIs)
                    return originalColor;

                return Color.Lerp(originalColor, Color.DarkCyan, 0.15f);
            });
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => ExoMechTileTileColor += MakeExoMechBgMoreCyan;

        public void Unload() => ExoMechTileTileColor -= MakeExoMechBgMoreCyan;
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

    public class ReplaceAbyssWorldgen : IHookEdit
    {
        internal static void ChangeAbyssGen(Action orig) => CustomAbyss.Generate();

        public void Load() => GenerateAbyss += ChangeAbyssGen;

        public void Unload() => GenerateAbyss -= ChangeAbyssGen;
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

    public class AdjustAbyssDefinitionHook : IHookEdit
    {
        internal bool ChangeAbyssRequirement(AbyssRequirementDelegate orig, Player player, out int playerYTileCoords)
        {
            Point point = player.Center.ToTileCoordinates();
            playerYTileCoords = point.Y;

            // Subworlds do not count as the abyss.
            if (WeakReferenceSupport.InAnySubworld())
                return false;
            
            // Check if the player is in the generous abyss area and has abyss walls behind them to determine if they are in the abyss.
            bool horizontalCheck;
            bool verticalCheck = point.Y <= Main.UnderworldLayer - 42 && point.Y > SulphurousSea.YStart + SulphurousSea.BlockDepth - 78;
            float yCompletion = Utils.GetLerpValue(CustomAbyss.AbyssTop, CustomAbyss.AbyssBottom - 1f, player.Center.Y / 16f, true);
            int abyssWidth = CustomAbyss.GetWidth(yCompletion, CustomAbyss.MinAbyssWidth, CustomAbyss.MaxAbyssWidth);
            if (Abyss.AtLeftSideOfWorld)
                horizontalCheck = point.X < abyssWidth;
            else
                horizontalCheck = point.X > Main.maxTilesX - abyssWidth;
            
            return !player.lavaWet && !player.honeyWet && verticalCheck && horizontalCheck;
        }

        internal bool ChangeLayer1Requirement(Func<AbyssLayer1Biome, Player, bool> orig, AbyssLayer1Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords <= CustomAbyss.Layer2Top;
            }

            return orig(self, player);
        }

        internal bool ChangeLayer2Requirement(Func<AbyssLayer2Biome, Player, bool> orig, AbyssLayer2Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer2Top && playerYTileCoords <= CustomAbyss.Layer3Top;
            }

            return orig(self, player);
        }

        internal bool ChangeLayer3Requirement(Func<AbyssLayer3Biome, Player, bool> orig, AbyssLayer3Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer3Top && playerYTileCoords <= CustomAbyss.Layer4Top;
            }

            return orig(self, player);
        }

        internal bool ChangeLayer4Requirement(Func<AbyssLayer4Biome, Player, bool> orig, AbyssLayer4Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer4Top && playerYTileCoords <= CustomAbyss.AbyssBottom;
            }
            
            return orig(self, player);
        }

        internal void ChangeAbyssWaterType(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.EmitDelegate(() => !WorldSaveSystem.InPostAEWUpdateWorld ? ModContent.Find<ModWaterStyle>("CalamityMod/AbyssWater") : ModContent.Find<ModWaterStyle>("InfernumMode/AbyssWater"));
            cursor.Emit(OpCodes.Ret);
        }

        public void Load()
        {
            MeetsBaseAbyssRequirement += ChangeAbyssRequirement;
            IsAbyssLayer1BiomeActive += ChangeLayer1Requirement;
            IsAbyssLayer2BiomeActive += ChangeLayer2Requirement;
            IsAbyssLayer3BiomeActive += ChangeLayer3Requirement;
            IsAbyssLayer4BiomeActive += ChangeLayer4Requirement;
            AbyssLayer1Color += ChangeAbyssWaterType;
            AbyssLayer2Color += ChangeAbyssWaterType;
            AbyssLayer3Color += ChangeAbyssWaterType;
            AbyssLayer4Color += ChangeAbyssWaterType;
        }

        public void Unload()
        {
            MeetsBaseAbyssRequirement -= ChangeAbyssRequirement;
            IsAbyssLayer1BiomeActive -= ChangeLayer1Requirement;
            IsAbyssLayer2BiomeActive -= ChangeLayer2Requirement;
            IsAbyssLayer3BiomeActive -= ChangeLayer3Requirement;
            IsAbyssLayer4BiomeActive -= ChangeLayer4Requirement;
            AbyssLayer1Color -= ChangeAbyssWaterType;
            AbyssLayer2Color -= ChangeAbyssWaterType;
            AbyssLayer3Color -= ChangeAbyssWaterType;
            AbyssLayer4Color -= ChangeAbyssWaterType;
        }
    }

    public class MakeMapGlitchInLayer4AbyssHook : IHookEdit
    {
        internal void CreateMapGlitchEffect(ILContext il)
        {
            ILCursor cursor = new(il);
            MethodInfo colorFloatMultiply = typeof(Color).GetMethod("op_Multiply", new Type[] { typeof(Color), typeof(float) });
            ConstructorInfo colorConstructor = typeof(Color).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) });

            // ==== APPLY EFFECT TO FULLSCREEN MAP =====

            // Find the map background draw method and use it as a hooking reference.
            if (!cursor.TryGotoNext(i => i.MatchCall<Main>("DrawMapFullscreenBackground")))
                return;

            // Go to the next 3 instances of Color.White being loaded and multiply them by the opacity factor.
            for (int i = 0; i < 3; i++)
            {
                if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<Color>("get_White")))
                    continue;

                cursor.EmitDelegate(() => 1f - Main.LocalPlayer.Infernum().MapObscurityInterpolant);
                cursor.Emit(OpCodes.Call, colorFloatMultiply);
            }

            // ==== APPLY EFFECT TO MAP RENDER TARGETS =====

            // Move after the map target color is decided, and multiply the result by the opacity factor/add blackness to it.
            if (!cursor.TryGotoNext(i => i.MatchLdfld<Main>("mapTarget")))
                return;
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchNewobj(colorConstructor)))
                return;

            cursor.EmitDelegate((Color c) =>
            {
                float obscurityInterpolant = Main.LocalPlayer.Infernum().MapObscurityInterpolant;
                if (Main.mapFullscreen)
                    return c * (1f - obscurityInterpolant);

                return Color.Lerp(c, Color.Black, obscurityInterpolant);
            });
        }

        public void Load() => IL.Terraria.Main.DrawMap += CreateMapGlitchEffect;

        public void Unload() => IL.Terraria.Main.DrawMap -= CreateMapGlitchEffect;
    }

    public class PreventAbyssDungeonInteractionsHook : IHookEdit
    {
        internal static void FixAbyssDungeonInteractions(ILContext il)
        {
            // Prevent the Dungeon's halls from getting anywhere near the Abyss.
            var cursor = new ILCursor(il);

            // Forcefully clamp the X position of the new hall end.
            // This prevents a hall, and as a result, the dungeon, from ever impeding on the Abyss/Sulph Sea.
            for (int k = 0; k < 2; k++)
            {
                if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(6)))
                    return;
            }

            cursor.Emit(OpCodes.Ldloc, 6);
            cursor.EmitDelegate<Func<Vector2, Vector2>>(unclampedValue =>
            {
                unclampedValue.X = MathHelper.Clamp(unclampedValue.X, CustomAbyss.MaxAbyssWidth + 25, Main.maxTilesX - CustomAbyss.MaxAbyssWidth - 25);
                return unclampedValue;
            });
            cursor.Emit(OpCodes.Stloc, 6);
        }

        public void Load() => IL.Terraria.WorldGen.DungeonHalls += FixAbyssDungeonInteractions;

        public void Unload() => IL.Terraria.WorldGen.DungeonHalls -= FixAbyssDungeonInteractions;
    }
}