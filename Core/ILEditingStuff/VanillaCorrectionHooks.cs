using CalamityMod;
using CalamityMod.Balancing;
using CalamityMod.BiomeManagers;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Schematics;
using CalamityMod.Skies;
using CalamityMod.Systems;
using CalamityMod.TileEntities;
using CalamityMod.World;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using InfernumMode.Content.Subworlds;
using InfernumMode.Content.Tiles.Relics;
using InfernumMode.Content.UI;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static CalamityMod.Events.BossRushEvent;
using static InfernumMode.Core.ILEditingStuff.HookManager;
using InfernumBalancingManager = InfernumMode.Core.Balancing.BalancingChangesManager;

namespace InfernumMode.Core.ILEditingStuff
{
    public class ReplaceGoresHook : IHookEdit
    {
        internal static List<int> InvalidGoreIDs = new()
        {
            // Adult Eidolon Wyrm.
            InfernumMode.CalamityMod.Find<ModGore>("PrimordialWyrm").Type,
            InfernumMode.CalamityMod.Find<ModGore>("PrimordialWyrm2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("PrimordialWyrm3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("PrimordialWyrm4").Type,

            // Aquatic Scourge.
            InfernumMode.CalamityMod.Find<ModGore>("ASBody").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASBody2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASBody3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASBody4").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASBodyAlt").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASBodyAlt2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASBodyAlt3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASHead").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASTail").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ASTail2").Type,

            // Calamitas' Shadow.
            InfernumMode.CalamityMod.Find<ModGore>("Calamitas").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Calamitas2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Calamitas3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Calamitas4").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Calamitas5").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Calamitas6").Type,

            // Cataclysm.
            InfernumMode.CalamityMod.Find<ModGore>("Cataclysm").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Cataclysm2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Cataclysm3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Cataclysm4").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Cataclysm5").Type,

            // Catastrophe.
            InfernumMode.CalamityMod.Find<ModGore>("Catastrophe").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Catastrophe2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Catastrophe3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Catastrophe4").Type,
            InfernumMode.CalamityMod.Find<ModGore>("Catastrophe5").Type,

            // Cultist.
            GoreID.Cultist1,
            GoreID.Cultist2,
            GoreID.CultistBoss1,
            GoreID.CultistBoss2,

            // Deerclops,
            GoreID.DeerclopsHead,
            GoreID.DeerclopsAntler,
            GoreID.DeerclopsBody,
            GoreID.DeerclopsLeg,

            // Empress of Light.
            GoreID.HallowBoss1,
            GoreID.HallowBoss2,
            GoreID.HallowBoss3,
            GoreID.HallowBoss4,
            GoreID.HallowBoss5,
            GoreID.HallowBoss6,
            GoreID.HallowBoss7,
        };

        internal static Dictionary<int, int> ReplacementTable = new()
        {
            // Devourer of Gods.
            [InfernumMode.CalamityMod.Find<ModGore>("DoGS").Type] = InfernumMode.Instance.Find<ModGore>("DoG1").Type,
            [InfernumMode.CalamityMod.Find<ModGore>("DoGS2").Type] = InfernumMode.Instance.Find<ModGore>("DoG2").Type,
            [InfernumMode.CalamityMod.Find<ModGore>("DoGS3").Type] = InfernumMode.Instance.Find<ModGore>("DoG3").Type,
            [InfernumMode.CalamityMod.Find<ModGore>("DoGS4").Type] = InfernumMode.Instance.Find<ModGore>("DoG4").Type,
            [InfernumMode.CalamityMod.Find<ModGore>("DoGS5").Type] = InfernumMode.Instance.Find<ModGore>("DoG5").Type,
            [InfernumMode.CalamityMod.Find<ModGore>("DoGS6").Type] = InfernumMode.Instance.Find<ModGore>("DoG6").Type,

            // Duke Fishron. These IDs do not have a corresponding GoreID constant.
            [573] = InfernumMode.Instance.Find<ModGore>("DukeFishronGore1").Type,
            [574] = InfernumMode.Instance.Find<ModGore>("DukeFishronGore3").Type,
            [575] = InfernumMode.Instance.Find<ModGore>("DukeFishronGore2").Type,
            [576] = InfernumMode.Instance.Find<ModGore>("DukeFishronGore4").Type,
        };

        internal static int AlterGores(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            // Do not spawn gores on the server.
            if (Main.netMode == NetmodeID.Server || Main.gamePaused)
                return Main.maxGore;

            if (InfernumMode.CanUseCustomAIs)
            {
                for (int i = 2; i <= 4; i++)
                {
                    if (Type == InfernumMode.CalamityMod.Find<ModGore>("Hive" + i).Type || Type == InfernumMode.CalamityMod.Find<ModGore>("Hive").Type)
                        return Main.maxGore;

                    else if (Type == InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossA" + i).Type || Type == InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossA").Type)
                        return Main.maxGore;
                }
            }

            if (InfernumMode.CanUseCustomAIs && ReplacementTable.TryGetValue(Type, out int replacementGoreID))
                Type = replacementGoreID;
            if (InfernumMode.CanUseCustomAIs && InvalidGoreIDs.Contains(Type))
                return Main.maxGore;

            return orig(source, Position, Velocity, Type, Scale);
        }

        public void Load() => On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += AlterGores;

        public void Unload() => On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float -= AlterGores;
    }

    public class MoveDraedonHellLabHook : IHookEdit
    {
        internal static void SlideOverHellLab(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.EmitDelegate(() =>
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

    public class MakeSulphSeaCavesBiggerHook : IHookEdit
    {
        internal static void MakeCavesBigger1(ILContext il)
        {
            ILCursor cursor = new(il);

            for (int i = 0; i < 2; i++)
            {
                cursor.GotoNext(MoveType.After, i => i.MatchLdsfld<SulphurousSea>("CheeseCaveCarveOutThresholds"));
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate(() => new float[]
                {
                    0.13f
                });
            }

            cursor.Goto(0);
            for (int i = 0; i < 2; i++)
            {
                cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(SulphurousSea.CheeseCaveMagnification));
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate(() => SulphurousSea.CheeseCaveMagnification * 0.3f);
            }
        }

        internal static void MakeCavesBigger2(ILContext il)
        {
            ILCursor cursor = new(il);

            for (int i = 0; i < 2; i++)
            {
                cursor.GotoNext(MoveType.After, i => i.MatchLdsfld<SulphurousSea>("SpaghettiCaveCarveOutThresholds"));
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate(() => new float[]
                {
                    0.033f,
                    0.125f
                });
            }

            cursor.Goto(0);
            for (int i = 0; i < 2; i++)
            {
                cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(SulphurousSea.SpaghettiCaveMagnification));
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate(() => SulphurousSea.SpaghettiCaveMagnification * 0.6f);
            }
        }

        public void Load()
        {
            GenerateSulphSeaCheeseCaves += MakeCavesBigger1;
            GenerateSulphSeaSpaghettiCaves += MakeCavesBigger2;
        }

        public void Unload()
        {
            GenerateSulphSeaCheeseCaves -= MakeCavesBigger1;
            GenerateSulphSeaSpaghettiCaves -= MakeCavesBigger2;
        }
    }

    public class ManipulateSunPositionHook : IHookEdit
    {
        public static Vector2 SunPosition
        {
            get;
            private set;
        }

        public static Main.SceneArea SunSceneArea
        {
            get;
            private set;
        }

        public static bool DisableSunForNextFrame
        {
            get;
            set;
        }

        internal void ForceDrawBlack(On_Main.orig_DrawBlack orig, Main self, bool force)
        {
            orig(self, force || LostColosseum.WasInColosseumLastFrame || CeaselessDimensionDrawSystem.BackgroundChangeInterpolant > 0f);
        }

        internal void ChangeDrawBlackLimit(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(x => x.MatchStloc(13)))
                return;

            c.Emit(OpCodes.Ldloc, 3);
            c.EmitDelegate<Func<float, float>>(lightThreshold =>
            {
                if (CeaselessDimensionDrawSystem.BackgroundChangeInterpolant > 0f)
                    return 0.00001f;

                if (SubworldSystem.IsActive<LostColosseum>())
                    return 0.125f;

                return lightThreshold;
            });
            c.Emit(OpCodes.Stloc, 3);
        }

        private void GetRidOfPeskyBlackSpaceFade(On_Main.orig_UpdateAtmosphereTransparencyToSkyColor orig)
        {
            Color oldSkyColor = Main.ColorOfTheSkies;
            orig();

            if (SubworldSystem.IsActive<LostColosseum>())
            {
                Main.atmo = 1f;
                Main.sunModY = 300;
                Main.ColorOfTheSkies = oldSkyColor;
            }
        }

        private void ChangeBackgroundColorSpecifically(ILContext il)
        {
            ILCursor c = new(il);

            if (!c.TryGotoNext(c => c.MatchStfld<Main>("unityMouseOver")))
                return;

            if (!c.TryGotoNext(c => c.MatchLdsfld<Main>("background")))
                return;

            int assetIndex = -1;
            if (!c.TryGotoNext(MoveType.After, c => c.MatchStloc(out assetIndex)))
                return;

            c.Emit(OpCodes.Ldloc, assetIndex);
            c.EmitDelegate((Asset<Texture2D> texture) =>
            {
                if (!Main.gameMenu && SubworldSystem.IsActive<LostColosseum>())
                    return ModContent.Request<Texture2D>("InfernumMode/Content/Backgrounds/LostColosseumSky");

                return texture;
            });
            c.Emit(OpCodes.Stloc, assetIndex);
        }

        private void DrawStrongerSunInColosseum(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            // Don't draw the moon if it's in use, or being drawn seperately.
            if (!Main.dayTime)
            {
                if (StolenCelestialObject.MoonIsNotInSky)
                    return;
                else if (!Main.gameMenu && Main.LocalPlayer.GetModPlayer<FlowerOceanPlayer>().VisualsActive)
                    return;
            }

            // Don't draw the sun if it's in use.
            if (Main.dayTime && StolenCelestialObject.SunIsNotInSky)
                return;

            if (EmpressUltimateAttackLightSystem.VerticalSunMoonOffset >= 0f)
            {
                sceneArea.bgTopY -= (int)EmpressUltimateAttackLightSystem.VerticalSunMoonOffset;
                EmpressUltimateAttackLightSystem.VerticalSunMoonOffset *= 0.96f;
            }

            bool inColosseum = !Main.gameMenu && SubworldSystem.IsActive<LostColosseum>();
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            float dayCompletion = (float)(Main.time / Main.dayLength);
            float verticalOffsetInterpolant;
            if (dayCompletion < 0.5f)
                verticalOffsetInterpolant = Pow(1f - dayCompletion * 2f, 2f);
            else
                verticalOffsetInterpolant = Pow(dayCompletion - 0.5f, 2f) * 4f;

            // Calculate the position of the sun.
            Texture2D sunTexture = TextureAssets.Sun.Value;
            int x = (int)(dayCompletion * sceneArea.totalWidth + sunTexture.Width * 2f) - sunTexture.Width;
            int y = (int)(sceneArea.bgTopY + verticalOffsetInterpolant * 250f + Main.sunModY);
            Vector2 sunPosition = new(x - 108f, y + 180f);
            SunSceneArea = sceneArea;
            SunPosition = sunPosition;

            if (DisableSunForNextFrame)
            {
                DisableSunForNextFrame = false;
                return;
            }

            if (!inColosseum)
            {
                // Draw a vibrant glow effect behind the sun if fighting the empress during the day.
                bool empressIsPresent = NPC.AnyNPCs(NPCID.HallowBoss) && InfernumMode.CanUseCustomAIs && Main.dayTime;
                if (empressIsPresent)
                {
                    // Use additive drawing.
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);

                    Vector2 origin = backglowTexture.Size() * 0.5f;
                    Main.spriteBatch.Draw(backglowTexture, sunPosition, null, Color.Cyan * 0.56f, 0f, origin, 4f, 0, 0f);
                    Main.spriteBatch.Draw(backglowTexture, sunPosition, null, Color.HotPink * 0.5f, 0f, origin, 8f, 0, 0f);
                    Main.spriteBatch.Draw(backglowTexture, sunPosition, null, Color.Lerp(Color.IndianRed, Color.Pink, 0.7f) * 0.4f, 0f, origin, 15f, 0, 0f);

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);
                }

                orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);

                if (empressIsPresent)
                {
                    // Use additive drawing.
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);

                    Vector2 origin = backglowTexture.Size() * 0.5f;
                    Color transColor = Color.Lerp(Color.HotPink, Color.Cyan, Sin(Main.GlobalTimeWrappedHourly * 1.1f) * 0.5f + 0.5f);
                    Main.spriteBatch.Draw(backglowTexture, sunPosition, null, transColor, 0f, origin, 0.74f, 0, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);
                }

                return;
            }

            // Use brighter sun colors in general in the colosseum.
            if (inColosseum)
                sunColor = Color.Lerp(sunColor, Color.White with { A = 125 }, 0.6f);

            // Draw a vibrant glow effect behind the sun if in the colosseum.
            if (inColosseum)
            {
                // Use additive drawing.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);

                Vector2 origin = backglowTexture.Size() * 0.5f;
                float opacity = Utils.GetLerpValue(0.67f, 1f, LostColosseum.SunsetInterpolant);
                Main.spriteBatch.Draw(backglowTexture, sunPosition, null, Color.Yellow * opacity * 0.5f, 0f, origin, 3f, 0, 0f);
                Main.spriteBatch.Draw(backglowTexture, sunPosition, null, Color.Orange * opacity * 0.56f, 0f, origin, 6f, 0, 0f);
                Main.spriteBatch.Draw(backglowTexture, sunPosition, null, Color.IndianRed * opacity * 0.46f, 0f, origin, 12f, 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);
            }

            sceneArea.bgTopY -= 180;
            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
        }

        public void Load()
        {
            Main.QueueMainThreadAction(() =>
            {
                On_Main.DrawBlack += ForceDrawBlack;
                IL_Main.DrawBlack += ChangeDrawBlackLimit;
                On_Main.UpdateAtmosphereTransparencyToSkyColor += GetRidOfPeskyBlackSpaceFade;
                IL_Main.DoDraw += ChangeBackgroundColorSpecifically;
                On_Main.DrawSunAndMoon += DrawStrongerSunInColosseum;
            });
        }

        public void Unload()
        {
            Main.QueueMainThreadAction(() =>
            {
                On_Main.DrawBlack -= ForceDrawBlack;
                IL_Main.DrawBlack -= ChangeDrawBlackLimit;
                On_Main.UpdateAtmosphereTransparencyToSkyColor -= GetRidOfPeskyBlackSpaceFade;
                IL_Main.DoDraw -= ChangeBackgroundColorSpecifically;
                On_Main.DrawSunAndMoon -= DrawStrongerSunInColosseum;
            });
        }
    }

    public class GetRidOfOnHitDebuffsHook : IHookEdit
    {
        internal static void EarlyReturn(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load()
        {
            YharonOnHitPlayer += EarlyReturn;
            SCalOnHitPlayer += EarlyReturn;
        }

        public void Unload()
        {
            YharonOnHitPlayer -= EarlyReturn;
            SCalOnHitPlayer -= EarlyReturn;
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
        internal static void EarlyReturn(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ret);
        }

        public void Load() => SpawnProvLootBox += EarlyReturn;

        public void Unload() => SpawnProvLootBox -= EarlyReturn;
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
            c.EmitDelegate(() => DifficultyManagementSystem.DisableDifficultyModes ? "Mods.InfernumMode.UI.NotExpertWarning" : "UI.WorldDescriptionNormal");

            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdstr("UI.WorldDescriptionMaster")))
                return;

            // Pop original value off.
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => DifficultyManagementSystem.DisableDifficultyModes ? "Mods.InfernumMode.UI.NotExpertWarning" : "UI.WorldDescriptionMaster");
        }

        public void Load() => Terraria.GameContent.UI.States.IL_UIWorldCreation.AddWorldDifficultyOptions += SwapDescriptionKeys;

        public void Unload() => Terraria.GameContent.UI.States.IL_UIWorldCreation.AddWorldDifficultyOptions -= SwapDescriptionKeys;
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
        internal static bool LetAureusWalkOnPlatforms(On_NPC.orig_Collision_DecideFallThroughPlatforms orig, NPC npc)
        {
            if (npc.type == ModContent.NPCType<AstrumAureus>())
            {
                if (Main.player[npc.target].position.Y > npc.Bottom.Y)
                    return true;
                return false;
            }
            return orig(npc);
        }

        public void Load() => On_NPC.Collision_DecideFallThroughPlatforms += LetAureusWalkOnPlatforms;

        public void Unload() => On_NPC.Collision_DecideFallThroughPlatforms -= LetAureusWalkOnPlatforms;
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

        public void Load() => Terraria.GameContent.Events.IL_ScreenDarkness.Update += AdjustFishronScreenDistanceRequirement;

        public void Unload() => Terraria.GameContent.Events.IL_ScreenDarkness.Update -= AdjustFishronScreenDistanceRequirement;
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

        public void Load() => IL_Main.UpdateTime_StartNight += ChangeEoCHPRequirements;

        public void Unload() => IL_Main.UpdateTime_StartNight -= ChangeEoCHPRequirements;
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

        private void OptionallyDisableKSSpawn(On_NPC.orig_SpawnOnPlayer orig, int plr, int Type)
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
            IL_NPC.SpawnNPC += ChangeKSHPRequirements;
            On_NPC.SpawnOnPlayer += OptionallyDisableKSSpawn;
        }

        public void Unload()
        {
            IL_NPC.SpawnNPC -= ChangeKSHPRequirements;
            On_NPC.SpawnOnPlayer -= OptionallyDisableKSSpawn;
        }
    }

    public class UseCustomShineParticlesForInfernumParticlesHook : IHookEdit
    {
        internal static void EmitFireParticles(On_TileDrawing.orig_DrawTiles_EmitParticles orig, TileDrawing self, int j, int i, Tile tileCache, ushort typeCache, short tileFrameX, short tileFrameY, Color tileLight)
        {
            ModTile mt = TileLoader.GetTile(tileCache.TileType);
            if ((tileLight.R > 20 || tileLight.B > 20 || tileLight.G > 20) && Main.rand.NextBool(12) && mt is not null and BaseInfernumBossRelic)
            {
                Dust fire = Dust.NewDustDirect(new Vector2(i * 16, j * 16), 16, 16, Main.rand.NextBool() ? 267 : 6, 0f, 0f, 254, Color.White, 1.4f);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.5f);
                fire.color = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.7f));
                fire.noGravity = true;
            }

            // I don't know who fucked this up. I don't know if it was me.
            // But I'm sick of my game going to 1 FPS due to hundreds of exceptions being thrown every single frame and as such will be the one
            // to fix it.
            if (tileCache.TileType is not TileID.LeafBlock and not TileID.LivingMahoganyLeaves)
                orig(self, i, j, tileCache, typeCache, tileFrameX, tileFrameY, tileLight);
        }

        public void Load() => On_TileDrawing.DrawTiles_EmitParticles += EmitFireParticles;

        public void Unload() => On_TileDrawing.DrawTiles_EmitParticles -= EmitFireParticles;
    }

    public class ReplaceAbyssWorldgen : IHookEdit
    {
        internal static void ChangeAbyssGen(Action orig) => CustomAbyss.Generate();

        public void Load() => GenerateAbyss += ChangeAbyssGen;

        public void Unload() => GenerateAbyss -= ChangeAbyssGen;
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
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, scourgeID);

                // Summon nuisances if not in Infernum mode.
                if (CalamityWorld.revenge && !InfernumMode.CanUseCustomAIs)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());
                    else
                        NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());
                    else
                        NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<DesertNuisanceHead>());
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

        public void Load()
        {
            MeetsBaseAbyssRequirement += ChangeAbyssRequirement;
            IsAbyssLayer1BiomeActive += ChangeLayer1Requirement;
            IsAbyssLayer2BiomeActive += ChangeLayer2Requirement;
            IsAbyssLayer3BiomeActive += ChangeLayer3Requirement;
            IsAbyssLayer4BiomeActive += ChangeLayer4Requirement;
        }

        public void Unload()
        {
            MeetsBaseAbyssRequirement -= ChangeAbyssRequirement;
            IsAbyssLayer1BiomeActive -= ChangeLayer1Requirement;
            IsAbyssLayer2BiomeActive -= ChangeLayer2Requirement;
            IsAbyssLayer3BiomeActive -= ChangeLayer3Requirement;
            IsAbyssLayer4BiomeActive -= ChangeLayer4Requirement;
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

                cursor.EmitDelegate(() => 1f - Main.LocalPlayer.Infernum_Biome().MapObscurityInterpolant);
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
                float obscurityInterpolant = Main.LocalPlayer.Infernum_Biome().MapObscurityInterpolant;
                if (Main.mapFullscreen)
                    return c * (1f - obscurityInterpolant);

                return Color.Lerp(c, Color.Black, obscurityInterpolant);
            });
        }

        public void Load() => IL_Main.DrawMap += CreateMapGlitchEffect;

        public void Unload() => IL_Main.DrawMap -= CreateMapGlitchEffect;
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
                unclampedValue.X = Clamp(unclampedValue.X, CustomAbyss.MaxAbyssWidth + 150, Main.maxTilesX - CustomAbyss.MaxAbyssWidth - 150);
                return unclampedValue;
            });
            cursor.Emit(OpCodes.Stloc, 6);
        }

        public void Load() => IL_WorldGen.DungeonHalls += FixAbyssDungeonInteractions;

        public void Unload() => IL_WorldGen.DungeonHalls -= FixAbyssDungeonInteractions;
    }

    public class ChangeBRSkyColorHook : IHookEdit
    {
        public void Load() => BRSkyColor += ChangeBRSkyColor;

        public void Unload() => BRSkyColor -= ChangeBRSkyColor;

        private void ChangeBRSkyColor(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.EmitDelegate(() =>
            {
                Color color = Color.Lerp(new Color(205, 100, 100), Color.Black, WhiteDimness) * 0.2f;
                return color;
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    public class ChangeBREyeTextureHook : IHookEdit
    {
        public void Load() => BRXerocEyeTexure += ChangeBREyeTexture;

        public void Unload() => BRXerocEyeTexure -= ChangeBREyeTexture;

        private void ChangeBREyeTexture(ILContext il)
        {
            // Better to rewrite the entire thing to get it looking just right.
            ILCursor cursor = new(il);
            cursor.GotoNext(MoveType.Before, i => i.MatchLdstr("CalamityMod/Skies/XerocEye"));
            cursor.EmitDelegate(() =>
            {
                if (Main.gameMenu)
                    return;

                Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                screenCenter += new Vector2(Main.screenWidth, Main.screenHeight) * (Main.GameViewMatrix.Zoom - Vector2.One) * 0.5f;

                float scale = Lerp(0.8f, 0.9f, BossRushSky.IncrementalInterest) + Sin(BossRushSky.IdleTimer) * 0.01f;
                Vector2 drawPosition = (new Vector2(Main.LocalPlayer.Center.X, 1120f) - screenCenter) * 0.097f + screenCenter - Main.screenPosition - Vector2.UnitY * 100f;
                Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/XerocEyeAlt").Value;
                Color baseColorDraw = Color.Lerp(Color.White, Color.Red, BossRushSky.IncrementalInterest);

                Main.spriteBatch.Draw(eyeTexture, drawPosition, null, baseColorDraw, 0f, eyeTexture.Size() * 0.5f, scale, 0, 0f);

                Color fadedColor = Color.Lerp(baseColorDraw, Color.Red, 0.3f) * Lerp(0.18f, 0.3f, BossRushSky.IncrementalInterest);
                fadedColor.A = 0;

                float backEyeOutwardness = Lerp(8f, 4f, BossRushSky.IncrementalInterest);
                int backInstances = (int)Lerp(6f, 24f, BossRushSky.IncrementalInterest);
                for (int i = 0; i < backInstances; i++)
                {
                    Vector2 drawOffset = (TwoPi * 4f * i / backInstances + Main.GlobalTimeWrappedHourly * 2.1f).ToRotationVector2() * backEyeOutwardness;
                    Main.spriteBatch.Draw(eyeTexture, drawPosition + drawOffset, null, fadedColor * 0.3f, 0f, eyeTexture.Size() * 0.5f, scale, 0, 0f);
                }

                if (BossRushSky.ShouldDrawRegularly)
                    BossRushSky.ShouldDrawRegularly = false;
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    public class DeleteStupidScreenShadersHook : IHookEdit
    {
        public void Load()
        {
            Terraria.Graphics.Effects.On_FilterManager.CanCapture += NoScreenShader;
            SCalSkyDraw += ChangeSCalSkyRequirements;
            CalCloneSkyDraw += ChangeCalCloneSkyRequirements;
            YharonSkyDraw += ChangeYharonSkyRequirements;
        }

        public void Unload()
        {
            Terraria.Graphics.Effects.On_FilterManager.CanCapture -= NoScreenShader;
            SCalSkyDraw -= ChangeSCalSkyRequirements;
            CalCloneSkyDraw -= ChangeCalCloneSkyRequirements;
        }

        private void ChangeSCalSkyRequirements(Action<SCalBackgroundScene, Player, bool> orig, SCalBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs)
                return;

            orig(instance, player, isActive);
        }

        private void ChangeCalCloneSkyRequirements(Action<CalamitasCloneBackgroundScene, Player, bool> orig, CalamitasCloneBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs)
                return;

            orig(instance, player, isActive);
        }

        private bool NoScreenShader(Terraria.Graphics.Effects.On_FilterManager.orig_CanCapture orig, Terraria.Graphics.Effects.FilterManager self)
        {
            if (CosmicBackgroundSystem.EffectIsActive)
                return false;

            return orig(self);
        }

        private void ChangeYharonSkyRequirements(Action<YharonBackgroundScene, Player, bool> orig, YharonBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs && !InfernumConfig.Instance.ReducedGraphicsConfig)
                return;

            orig(instance, player, isActive);
        }
    }

    public class AdjustASWaterPoisonTimersHook : IHookEdit
    {
        public void Load()
        {
            UpdateBadLifeRegen += AdjustTimers;
        }

        public void Unload()
        {
            UpdateBadLifeRegen -= AdjustTimers;
        }

        private void AdjustTimers(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.EmitDelegate(() =>
            {
                if (!MakeSulphSeaWaterEasierToSeeInHook.CanUseHighQualityWater)
                    WaterClearingBubble.ClaimAllBubbles();
                if (NPC.AnyNPCs(ModContent.NPCType<AquaticScourgeHead>()) && InfernumMode.CanUseCustomAIs)
                    Main.LocalPlayer.Calamity().decayEffigy = false;
            });

            int poisonIncrementIndex = 0;
            cursor.GotoNext(i => i.MatchLdcR4(1f / CalamityPlayer.SulphSeaWaterSafetyTime));
            cursor.GotoNext(i => i.MatchStloc(out poisonIncrementIndex));

            // Multiply the poison increment by a predetermined factor during the Aquatic Scourge fight, so that it's more fair overall.
            cursor.GotoNext(i => i.MatchLdfld<CalamityPlayer>("SulphWaterPoisoningLevel"));
            cursor.GotoNext(MoveType.After, i => i.MatchLdloc(poisonIncrementIndex));

            cursor.EmitDelegate(() =>
            {
                int aquaticScourgeIndex = NPC.FindFirstNPC(ModContent.NPCType<AquaticScourgeHead>());
                if (aquaticScourgeIndex >= 0 && Main.npc[aquaticScourgeIndex].ai[2] != (int)AquaticScourgeHeadBehaviorOverride.AquaticScourgeAttackType.DeathAnimation && InfernumMode.CanUseCustomAIs)
                {
                    NPC scourge = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<AquaticScourgeHead>())];
                    Player player = Main.LocalPlayer;
                    float acidVerticalLine = scourge.Infernum().ExtraAI[AquaticScourgeHeadBehaviorOverride.AcidVerticalLineIndex];
                    if (acidVerticalLine > 0f && player.Top.Y >= acidVerticalLine)
                        return AquaticScourgeHeadBehaviorOverride.PoisonChargeUpSpeedFactorFinalPhase;

                    return AquaticScourgeHeadBehaviorOverride.PoisonChargeUpSpeedFactor;
                }

                return 1f;
            });
            cursor.Emit(OpCodes.Mul);

            // Redecide poison decrement by a predetermined factor during the Aquatic Scourge fight, so that it's more fair overall.
            cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(1f / CalamityPlayer.SulphSeaWaterRecoveryTime));
            cursor.Emit(OpCodes.Pop);

            cursor.EmitDelegate(() =>
            {
                int recoveryTime = CalamityPlayer.SulphSeaWaterRecoveryTime;
                if (NPC.AnyNPCs(ModContent.NPCType<AquaticScourgeHead>()) && InfernumMode.CanUseCustomAIs)
                    recoveryTime = (int)(recoveryTime / AquaticScourgeHeadBehaviorOverride.PoisonFadeOutSpeedFactor);

                return 1f / recoveryTime;
            });
        }
    }

    public class MakeDungeonSpawnAtLeftSideHook : IHookEdit
    {
        // This is so hideous but the alternative is IL editing on anonymous methods.
        internal static bool ReturnZeroInRandomness;

        public void Load()
        {
            On_WorldGen.RandomizeMoonState += PrepareDungeonSide;
            On_UnifiedRandom.Next_int += HijackRNG;
        }

        public void Unload()
        {
            On_WorldGen.RandomizeMoonState -= PrepareDungeonSide;
            On_UnifiedRandom.Next_int -= HijackRNG;
        }

        private void PrepareDungeonSide(On_WorldGen.orig_RandomizeMoonState orig, UnifiedRandom random, bool garenteeNewStyle)
        {
            orig(random, garenteeNewStyle);
            ReturnZeroInRandomness = true;
        }

        private int HijackRNG(On_UnifiedRandom.orig_Next_int orig, UnifiedRandom self, int maxValue)
        {
            if (ReturnZeroInRandomness)
            {
                ReturnZeroInRandomness = false;
                return 0;
            }

            // In MP, this is sometimes less than 0?? Not sure why, but make sure that doesn't happen.
            if (maxValue < 0)
                maxValue *= -1;

            return orig(self, maxValue);
        }
    }

    public class AllowTalkingToDraedonHook : IHookEdit
    {
        public void Load()
        {
            if (DraetingSimSystem.ShouldEnableDraedonDialog)
            {
                DrawCodebreakerUI += ChangeTalkCondition;
                DisplayCodebreakerCommunicationPanel += DrawCustomDialogPanel;
            }
        }

        public void Unload()
        {
            if (DraetingSimSystem.ShouldEnableDraedonDialog)
            {
                DrawCodebreakerUI -= ChangeTalkCondition;
                DisplayCodebreakerCommunicationPanel -= DrawCustomDialogPanel;
            }
        }

        private void ChangeTalkCondition(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchCallOrCallvirt<TECodebreaker>("get_ReadyToSummonDraedon"));

            for (int i = 0; i < 2; i++)
                cursor.GotoNext(j => j.MatchStloc(out _));

            cursor.GotoPrev(MoveType.After, i => i.MatchLdcI4(0));
            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(() => DownedBossSystem.downedExoMechs.ToInt());
        }

        private void DrawCustomDialogPanel(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.EmitDelegate(InfernumDraedonDialog.DisplayCommunicationPanel);
            cursor.Emit(OpCodes.Ret);
        }
    }

    public class DrawNightStarsHook : IHookEdit
    {
        public void Load() => On_Main.DrawStarsInBackground += DrawStarsHook;

        public void Unload() => On_Main.DrawStarsInBackground -= DrawStarsHook;

        private void DrawStarsHook(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
        {
            // Do not draw if the flower ocean visuals are active.
            if (!Main.gameMenu && Main.LocalPlayer.GetModPlayer<FlowerOceanPlayer>().VisualsActive)
                return;

            orig(self, sceneArea, artificial);
        }
    }

    public class DisableWaterDrawingDuringAEWHook : IHookEdit
    {
        public void Load() => On_Main.DrawWaters += DisableWaterDrawing;

        public void Unload() => On_Main.DrawWaters -= DisableWaterDrawing;

        private void DisableWaterDrawing(On_Main.orig_DrawWaters orig, Main self, bool isBackground)
        {
            if (InfernumMode.CanUseCustomAIs && Main.LocalPlayer.Calamity().ZoneAbyssLayer4)
                return;

            orig(self, isBackground);
        }
    }

    public class ChangeCalCloneNameHook : IHookEdit
    {
        public void Load() => On_NPC.DoDeathEvents_DropBossPotionsAndHearts += ChangeName;

        public void Unload() => On_NPC.DoDeathEvents_DropBossPotionsAndHearts -= ChangeName;

        private void ChangeName(On_NPC.orig_DoDeathEvents_DropBossPotionsAndHearts orig, NPC npc, ref string typeName)
        {
            orig(npc, ref typeName);
            if (npc.type == ModContent.NPCType<CalamitasClone>() && InfernumMode.CanUseCustomAIs)
                typeName = $"The {CalamitasShadowBehaviorOverride.CustomName}";
        }
    }

    public class MakeEternityOPHook : IHookEdit
    {
        public void Load() => EternityHexAI += ChangeDamageValues;

        public void Unload() => EternityHexAI -= ChangeDamageValues;

        private void ChangeDamageValues(ILContext il)
        {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(i => i.MatchCallOrCallvirt<StatModifier>("ApplyTo")))
            {
                cursor.GotoNext(MoveType.Before, i => i.MatchLdcR4(out float originalDamage));
                cursor.Remove();
                cursor.EmitDelegate(() => (float)ItemDamageValues.DamageValues[ModContent.ItemType<Eternity>()]);
            }
        }
    }

    public class AntiFlashbangSupportHook : IHookEdit
    {
        public void Load() => IL_MoonlordDeathDrama.DrawWhite += IL_MoonlordDeathDrama_DrawWhite;

        public void Unload() => IL_MoonlordDeathDrama.DrawWhite -= IL_MoonlordDeathDrama_DrawWhite;

        // Do NOT remove this method, despite its 0 references it is used by IL.
        public static Color DrawColor() => InfernumConfig.Instance.FlashbangOverlays ? Color.White : new Color(5, 5, 5);

        private void IL_MoonlordDeathDrama_DrawWhite(ILContext il)
        {
            ILCursor cursor = new(il);

            // Replace the white color with a gray one, if the flashbang config is disbaled.
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchCall<Color>("get_White")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.EmitCall(typeof(AntiFlashbangSupportHook).GetMethod("DrawColor", Utilities.UniversalBindingFlags));
            }
        }
    }
}
