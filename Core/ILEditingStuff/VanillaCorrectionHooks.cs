using CalamityMod;
using CalamityMod.Balancing;
using CalamityMod.BiomeManagers;
using CalamityMod.CalPlayer;
using CalamityMod.Events;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Projectiles.Magic;
using CalamityMod.Schematics;
using CalamityMod.Skies;
using CalamityMod.Systems;
using CalamityMod.World;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using InfernumMode.Content.Subworlds;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI.States;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static CalamityMod.Events.BossRushEvent;
using InfernumBalancingManager = InfernumMode.Core.Balancing.BalancingChangesManager;

namespace InfernumMode.Core.ILEditingStuff
{
    internal sealed class ReplaceGoresHook : ModSystem
    {
        public static List<int> InvalidGoreIDs =
        [
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

            // Deerclops.
            GoreID.DeerclopsHead,
            GoreID.DeerclopsAntler,
            GoreID.DeerclopsBody,
            GoreID.DeerclopsLeg,
            GoreID.DeerclopsArm,

            // Empress of Light.
            GoreID.HallowBoss1,
            GoreID.HallowBoss2,
            GoreID.HallowBoss3,
            GoreID.HallowBoss4,
            GoreID.HallowBoss5,
            GoreID.HallowBoss6,
            GoreID.HallowBoss7,

            // King Slime.
            GoreID.KingSlimeCrown,

            // Skeletron. Yes these do not have gore IDs. I do not know why the IDs are incomplete.
            54,
            55,

            // Retinazer.
            143,
            146,
            
            // Spazmatism.
            144,
            145,

            // Brimstone Elemental.
            InfernumMode.CalamityMod.Find<ModGore>("BrimstoneGore1").Type,
            InfernumMode.CalamityMod.Find<ModGore>("BrimstoneGore2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("BrimstoneGore3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("BrimstoneGore4").Type,

            // Guardian healer.
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossH").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossH2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossH3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossH4").Type,

            // Guardian defender.
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossT").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossT2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossT3").Type,
            InfernumMode.CalamityMod.Find<ModGore>("ProfanedGuardianBossT4").Type,

            // Ceaseless void.
            InfernumMode.CalamityMod.Find<ModGore>("CeaselessVoid").Type,
            InfernumMode.CalamityMod.Find<ModGore>("CeaselessVoid2").Type,
            InfernumMode.CalamityMod.Find<ModGore>("CeaselessVoid3").Type,
        ];

        public static Dictionary<int, int> ReplacementTable = new()
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

        public override void Load() => On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += AlterGores;

        public static int AlterGores(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            // Do not spawn gores on the server.
            if (Main.netMode == NetmodeID.Server || Main.gamePaused)
                return Main.maxGore;

            if (InfernumMode.CanUseCustomAIs)
            {
                for (int i = 2; i <= 4; i++)
                {
                    if ((InfernumMode.CalamityMod.TryFind("Hive" + i, out ModGore HiveX) && HiveX.Type == Type) || (InfernumMode.CalamityMod.TryFind("Hive", out ModGore Hive) && Hive.Type == Type))
                        return Main.maxGore;

                    else if ((InfernumMode.CalamityMod.TryFind("ProfanedGuardianBossA" + i, out ModGore guardsAX) && guardsAX.Type == Type) || (InfernumMode.CalamityMod.TryFind("ProfanedGuardianBossA", out ModGore guardsA) && guardsA.Type == Type))
                        return Main.maxGore;
                }
            }

            if (InfernumMode.CanUseCustomAIs && ReplacementTable.TryGetValue(Type, out int replacementGoreID))
                Type = replacementGoreID;
            if (InfernumMode.CanUseCustomAIs && InvalidGoreIDs.Contains(Type))
                return Main.maxGore;

            return orig(source, Position, Velocity, Type, Scale);
        }
    }

    internal sealed class MoveDraedonHellLabHook : ModSystem
    {
        public static MethodInfo? PlaceHellLab = typeof(DraedonStructures).GetMethod("PlaceHellLab", Utilities.UniversalBindingFlags);
        public static ILHook? MoveDraedonHellLab_IL_Hook;

        public override void OnModLoad()
        {
            if (PlaceHellLab != null)
            {
                MoveDraedonHellLab_IL_Hook = new(PlaceHellLab, MoveDraedonHellLab_IL);
                MoveDraedonHellLab_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void MoveDraedonHellLab_IL(ILContext context)
        {
            ILCursor cursor = new(context);
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
                            Tile tile = Framing.GetTileSafely(x, y);
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
    }

    internal sealed class MakeCheeseSulphSeaCavesBiggerHook : ModSystem
    {
        public static MethodInfo? GenerateSulphSeaCheeseCaves = typeof(SulphurousSea).GetMethod("GenerateCheeseWaterCaves", Utilities.UniversalBindingFlags);
        public static ILHook? MakeCheeseSulphSeaCavesBigger_IL_Hook;

        public override void OnModLoad()
        {
            if (GenerateSulphSeaCheeseCaves != null)
            {
                MakeCheeseSulphSeaCavesBigger_IL_Hook = new(GenerateSulphSeaCheeseCaves, MakeCheeseSulphSeaCavesBigger_IL);
                MakeCheeseSulphSeaCavesBigger_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void MakeCheeseSulphSeaCavesBigger_IL(ILContext context)
        {
            ILCursor cursor = new(context);

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
    }

    internal sealed class MakeSpaghettiSulphSeaCavesBiggerHook : ModSystem
    {
        public static MethodInfo? GenerateSulphSeaSpaghettiCaves = typeof(SulphurousSea).GetMethod("GenerateSpaghettiWaterCaves", Utilities.UniversalBindingFlags);
        public static ILHook? MakeSpaghettiSulphSeaCavesBigger_IL_Hook;

        public override void OnModLoad()
        {
            if (GenerateSulphSeaSpaghettiCaves != null)
            {
                MakeSpaghettiSulphSeaCavesBigger_IL_Hook = new(GenerateSulphSeaSpaghettiCaves, MakeSpaghettiSulphSeaCavesBigger_IL);
                MakeSpaghettiSulphSeaCavesBigger_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void MakeSpaghettiSulphSeaCavesBigger_IL(ILContext context)
        {
            ILCursor cursor = new(context);

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
    }

    internal sealed class ManipulateSunPositionHook : ModSystem
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

        public override void Load()
        {
            On_Main.DrawBlack += ForceDrawBlack;
            On_Main.DrawSunAndMoon += DrawStrongerSunInColosseum;
            On_Main.UpdateAtmosphereTransparencyToSkyColor += GetRidOfPeskyBlackSpaceFade;


            IL_Main.DoDraw += ManipulateSunPosition_IL;
        }

        public static void ManipulateSunPosition_IL(ILContext context)
        {
            ILCursor c = new(context);

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

        internal void ForceDrawBlack(On_Main.orig_DrawBlack orig, Main self, bool force)
        {
            orig(self, force || LostColosseum.WasInColosseumLastFrame || CeaselessDimensionDrawSystem.BackgroundChangeInterpolant > 0f);
        }

        private static void GetRidOfPeskyBlackSpaceFade(On_Main.orig_UpdateAtmosphereTransparencyToSkyColor orig)
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

        private void DrawStrongerSunInColosseum(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            // Don't draw the moon if it's in use, or being drawn seperately.
            if (!Main.dayTime)
            {
                if (StolenCelestialObject.MoonIsNotInSky)
                    return;
                else if (!Main.gameMenu && Main.LocalPlayer.Infernum().GetValue<bool>("FlowerOceanVisualsActive"))
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
    }

    internal sealed class ChangeDrawBlackLimitHook : ModSystem
    {
        public override void Load() => IL_Main.DrawBlack += ChangeDrawBlackLimit_IL;

        public static void ChangeDrawBlackLimit_IL(ILContext context)
        {
            ILCursor c = new(context);
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
    }

    internal sealed class GetRidOfYharonOnHitDebuffsHook : ModSystem
    {
        public static MethodInfo? YharonOnHitPlayer = typeof(Yharon).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags);
        public static ILHook? GetRidOfYharonOnHitDebuffs_IL_Hook;

        public override void OnModLoad()
        {
            if (YharonOnHitPlayer != null)
            {
                GetRidOfYharonOnHitDebuffs_IL_Hook = new(YharonOnHitPlayer, GetRidOfYharonOnHitDebuffs_IL);
                GetRidOfYharonOnHitDebuffs_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void GetRidOfYharonOnHitDebuffs_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class GetRidOfSCalOnHitDebuffsHook : ModSystem
    {
        public static MethodInfo? SCalOnHitPlayer = typeof(SupremeCalamitas).GetMethod("OnHitPlayer", Utilities.UniversalBindingFlags);
        public static ILHook? GetRidOfSCalOnHitDebuffs_IL_Hook;

        public override void OnModLoad()
        {
            if (SCalOnHitPlayer != null)
            {
                GetRidOfSCalOnHitDebuffs_IL_Hook = new(SCalOnHitPlayer, GetRidOfSCalOnHitDebuffs_IL);
                GetRidOfSCalOnHitDebuffs_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void GetRidOfSCalOnHitDebuffs_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class ChangeBossRushTiersHook : ModSystem
    {
        public static MethodInfo? BossRushTier = typeof(BossRushEvent).GetMethod("get_CurrentTier", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeBossRushTiers_IL_Hook;

        public override void OnModLoad()
        {
            if (BossRushTier != null)
            {
                ChangeBossRushTiers_IL_Hook = new(BossRushTier, ChangeBossRushTiers_IL);
                ChangeBossRushTiers_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeBossRushTiers_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            cursor.EmitDelegate(() =>
            {
                // I'll leave this here if the boss rush can ever be swapped.
                /*
                int tier2Boss = NPCID.TheDestroyer;
                int tier3Boss = NPCID.CultistBoss;
                if (InfernumMode.CanUseCustomAIs)
                {
                    tier2Boss = ModContent.NPCType<ProfanedGuardianCommander>();
                    tier3Boss = ModContent.NPCType<SlimeGodCore>();
                }*/
                if (BossRushStage > Bosses.FindIndex((boss) => boss.EntityID == ModContent.NPCType<CalamitasClone>()))
                    return 5;
                if (BossRushStage > Bosses.FindIndex((boss) => boss.EntityID == NPCID.Golem || boss.EntityID == NPCID.GolemHead))
                    return 4;
                if (BossRushStage > Bosses.FindIndex((boss) => boss.EntityID == ModContent.NPCType<SlimeGodCore>()))
                    return 3;
                if (BossRushStage > Bosses.FindIndex((boss) => boss.EntityID == ModContent.NPCType<ProfanedGuardianCommander>()))
                    return 2;

                return 1;
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class ChangeExoMechBackgroundColorHook : ModSystem
    {
        public static MethodInfo? ExoMechTileTileColor = typeof(ExoMechsSky).GetMethod("OnTileColor", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeExoMechBackgroundColor_IL_Hook;

        public override void OnModLoad()
        {
            if (ExoMechTileTileColor != null)
            {
                ChangeExoMechBackgroundColor_IL_Hook = new(ExoMechTileTileColor, ChangeExoMechBackgroundColor_IL);
                ChangeExoMechBackgroundColor_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeExoMechBackgroundColor_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.GotoNext(MoveType.Before, i => i.MatchRet());

            cursor.EmitDelegate<Func<Color, Color>>(originalColor =>
            {
                if (!InfernumMode.CanUseCustomAIs)
                    return originalColor;

                return Color.Lerp(originalColor, Color.DarkCyan, 0.15f);
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class DisableExoMechsSkyInBRHook : ModSystem
    {
        public static MethodInfo? ExoMechsSkyIsActive = typeof(ExoMechsSky).GetMethod("get_CanSkyBeActive", Utilities.UniversalBindingFlags);
        public static ILHook? DisableExoMechsSkyInBR_IL_Hook;

        public override void OnModLoad()
        {
            if (ExoMechsSkyIsActive != null)
            {
                DisableExoMechsSkyInBR_IL_Hook = new(ExoMechsSkyIsActive, DisableExoMechsSkyInBR_IL);
                DisableExoMechsSkyInBR_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void DisableExoMechsSkyInBR_IL(ILContext context)
        {
            ILCursor cursor = new(context);

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
    }

    /*internal sealed class GetRidOfProvidenceLootBoxHook : ModSystem
    {
        public static ILHook? GetRidOfProvidenceLootBox_IL_Hook;

        public override void OnModLoad()
        {
            GetRidOfProvidenceLootBox_IL_Hook = new(SpawnProvLootBox, GetRidOfProvidenceLootBox_IL);
            GetRidOfProvidenceLootBox_IL_Hook?.Apply();
        }

        public static void GetRidOfProvidenceLootBox_IL(ILContext context) => HookHelper.EarlyReturnEdit(context, edit);
    }*/

    internal sealed class AddWarningAboutNonExpertOnWorldSelectionHook : ModSystem
    {
        public override void Load() => IL_UIWorldCreation.AddWorldDifficultyOptions += AddWarningAboutNonExpertOnWorldSelection_IL;

        public static void AddWarningAboutNonExpertOnWorldSelection_IL(ILContext context)
        {
            var c = new ILCursor(context);

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
    }

    internal sealed class ReducePlayerDashDelay : ModSystem
    {
        public static MethodInfo? DashMovement = typeof(CalamityPlayer).GetMethod("ModDashMovement", Utilities.UniversalBindingFlags);
        public static ILHook? ReducePlayerDashDelay_IL_Hook;

        public override void OnModLoad()
        {
            if (DashMovement != null)
            {
                ReducePlayerDashDelay_IL_Hook = new(DashMovement, ReducePlayerDashDelay_IL);
                ReducePlayerDashDelay_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ReducePlayerDashDelay_IL(ILContext context)
        {
            static int DashDelay() => InfernumMode.CanUseCustomAIs ? InfernumBalancingManager.DashDelay : BalancingConstants.UniversalDashCooldown;
            ILCursor c = new(context);
            c.GotoNext(MoveType.After, i => i.MatchLdcI4(BalancingConstants.UniversalDashCooldown));
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(DashDelay);
            //c.Emit(OpCodes.Ldc_I4, InfernumBalancingManager.DashDelay);

            c.GotoNext(MoveType.After, i => i.MatchLdcI4(BalancingConstants.UniversalShieldSlamCooldown));
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(DashDelay);
            //c.Emit(OpCodes.Ldc_I4, InfernumBalancingManager.DashDelay);

            c.GotoNext(MoveType.After, i => i.MatchLdcI4(BalancingConstants.UniversalShieldBonkCooldown));
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(DashDelay);
            //c.Emit(OpCodes.Ldc_I4, InfernumBalancingManager.DashDelay);
        }
    }

    internal sealed class AureusPlatformWalkingHook : ModSystem
    {
        public override void Load() => On_NPC.Collision_DecideFallThroughPlatforms += LetAureusWalkOnPlatforms;

        private static bool LetAureusWalkOnPlatforms(On_NPC.orig_Collision_DecideFallThroughPlatforms orig, NPC npc)
        {
            if (npc.type == ModContent.NPCType<AstrumAureus>())
            {
                if (Main.player[npc.target].position.Y > npc.Bottom.Y)
                    return true;
                return false;
            }
            return orig(npc);
        }
    }

    internal sealed class FishronSkyDistanceLeniancyHook : ModSystem
    {
        public override void Load() => IL_ScreenDarkness.Update += FishronSkyDistanceLeniancy_IL;

        public static void FishronSkyDistanceLeniancy_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.GotoNext(i => i.MatchLdcR4(3000f));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_R4, 6000f);
        }
    }

    internal sealed class EyeOfCthulhuSpawnHPMinChangeHook : ModSystem
    {
        public override void Load() => IL_Main.UpdateTime_StartNight += EyeOfCthulhuSpawnHPMinChange_IL;

        public static void EyeOfCthulhuSpawnHPMinChange_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.GotoNext(i => i.MatchLdcI4(200));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4, 400);
        }
    }

    internal sealed class KingSlimeSpawnHPMinChangeHook : ModSystem
    {
        private static bool spawningKingSlimeNaturally;

        public override void Load()
        {
            On_NPC.SpawnOnPlayer += OptionallyDisableKSSpawn;

            IL_NPC.SpawnNPC += KingSlimeSpawnHPMinChange_IL;
        }

        public static void KingSlimeSpawnHPMinChange_IL(ILContext context)
        {
            ILCursor cursor = new(context);
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
    }

    internal sealed class ReplaceAbyssWorldgen : ModSystem
    {
        public static MethodInfo? PlaceAbyssMethod = typeof(Abyss).GetMethod("PlaceAbyss", Utilities.UniversalBindingFlags);
        public delegate void Orig_Abyss_PlaceAbyss();
        public static Hook? ReplaceAbyssWorldgen_Detour_Hook;

        public override void OnModLoad()
        {
            if (PlaceAbyssMethod != null)
            {
                ReplaceAbyssWorldgen_Detour_Hook = new(PlaceAbyssMethod, ChangeAbyssGen);
                ReplaceAbyssWorldgen_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeAbyssGen(Orig_Abyss_PlaceAbyss orig)
        {
            if (InfernumConfig.Instance.CustomAbyssGeneration)
                CustomAbyss.Generate();
            else orig();
        }
    }

    internal sealed class GetRidOfDesertNuisancesHook : ModSystem
    {
        public static MethodInfo? DesertScourgeItemUseItem = typeof(DesertMedallion).GetMethod("UseItem", Utilities.UniversalBindingFlags);
        public static ILHook? GetRidOfDesertNuisances_IL_Hook;

        public override void OnModLoad()
        {
            if (DesertScourgeItemUseItem != null)
            {
                GetRidOfDesertNuisances_IL_Hook = new(DesertScourgeItemUseItem, GetRidOfDesertNuisances_IL);
                GetRidOfDesertNuisances_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void GetRidOfDesertNuisances_IL(ILContext context)
        {
            ILCursor cursor = new(context);
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
            cursor.Emit(OpCodes.Newobj, typeof(bool?).GetConstructor([typeof(bool)])!);
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class AdjustAbyssDefinitionHook : ModSystem
    {
        #region Hooks
        // Unused?
        //public delegate bool AbyssRequirementHookDelegate(Orig_AbyssLayer1Biome_MeetsBaseAbyssRequirement orig, Player player, out int playerYTileCoords);

        public static MethodInfo? MeetsBaseAbyssRequirementMethod = typeof(AbyssLayer1Biome).GetMethod("MeetsBaseAbyssRequirement", Utilities.UniversalBindingFlags);
        public delegate bool Orig_AbyssLayer1Biome_MeetsBaseAbyssRequirement(Player player, out int playerYTileCoords);
        public static Hook? ChangeAbyssRequirement_Detour_Hook;

        public static MethodInfo? IsBiomeActive1Method = typeof(AbyssLayer1Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags);
        public delegate bool Orig_AbyssLayer1Biome_IsBiomeActive1(AbyssLayer1Biome self, Player player);
        public static Hook? ChangeLayer1Requirement_Detour_Hook;

        public static MethodInfo? IsBiomeActive2Method = typeof(AbyssLayer2Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags);
        public delegate bool Orig_AbyssLayer2Biome_IsBiomeActive2(AbyssLayer2Biome self, Player player);
        public static Hook? ChangeLayer2Requirement_Detour_Hook;

        public static MethodInfo? IsBiomeActive3Method = typeof(AbyssLayer3Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags);
        public delegate bool Orig_AbyssLayer3Biome_IsBiomeActive3(AbyssLayer3Biome self, Player player);
        public static Hook? ChangeLayer3Requirement_Detour_Hook;

        public static MethodInfo? IsBiomeActive4Method = typeof(AbyssLayer4Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags);
        public delegate bool Orig_AbyssLayer4Biome_IsBiomeActive4(AbyssLayer4Biome self, Player player);
        public static Hook? ChangeLayer4Requirement_Detour_Hook;
        #endregion

        public override void OnModLoad()
        {
            if (MeetsBaseAbyssRequirementMethod != null)
            {
                ChangeAbyssRequirement_Detour_Hook = new(MeetsBaseAbyssRequirementMethod, ChangeAbyssRequirement);
                ChangeAbyssRequirement_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (IsBiomeActive1Method != null)
            {
                ChangeLayer1Requirement_Detour_Hook = new(IsBiomeActive1Method, ChangeLayer1Requirement);
                ChangeLayer1Requirement_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (IsBiomeActive2Method != null)
            {
                ChangeLayer2Requirement_Detour_Hook = new(IsBiomeActive2Method, ChangeLayer2Requirement);
                ChangeLayer2Requirement_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (IsBiomeActive3Method != null)
            {
                ChangeLayer3Requirement_Detour_Hook = new(IsBiomeActive3Method, ChangeLayer3Requirement);
                ChangeLayer3Requirement_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (IsBiomeActive4Method != null)
            {
                ChangeLayer4Requirement_Detour_Hook = new(IsBiomeActive4Method, ChangeLayer4Requirement);
                ChangeLayer4Requirement_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }
        public bool ChangeAbyssRequirement(Orig_AbyssLayer1Biome_MeetsBaseAbyssRequirement orig, Player player, out int playerYTileCoords)
        {
            if (!WorldSaveSystem.InPostAEWUpdateWorld)
                return orig(player, out playerYTileCoords);
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

        public bool ChangeLayer1Requirement(Orig_AbyssLayer1Biome_IsBiomeActive1 orig, AbyssLayer1Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords <= CustomAbyss.Layer2Top;
            }

            return orig(self, player);
        }

        public bool ChangeLayer2Requirement(Orig_AbyssLayer2Biome_IsBiomeActive2 orig, AbyssLayer2Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer2Top && playerYTileCoords <= CustomAbyss.Layer3Top;
            }

            return orig(self, player);
        }

        public bool ChangeLayer3Requirement(Orig_AbyssLayer3Biome_IsBiomeActive3 orig, AbyssLayer3Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer3Top && playerYTileCoords <= CustomAbyss.Layer4Top;
            }

            return orig(self, player);
        }

        public bool ChangeLayer4Requirement(Orig_AbyssLayer4Biome_IsBiomeActive4 orig, AbyssLayer4Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer4Top && playerYTileCoords <= CustomAbyss.AbyssBottom;
            }

            return orig(self, player);
        }
    }

    internal sealed class MakeMapGlitchInLayer4AbyssHook : ModSystem
    {
        public override void Load() => IL_Main.DrawMap += MakeMapGlitchInLayer4Abyss_IL;

        public static void MakeMapGlitchInLayer4Abyss_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            MethodInfo colorFloatMultiply = typeof(Color).GetMethod("op_Multiply", [typeof(Color), typeof(float)])!;
            ConstructorInfo colorConstructor = typeof(Color).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(int)])!;

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
    }

    internal sealed class PreventAbyssDungeonInteractionsHook : ModSystem
    {
        public override void Load() => IL_WorldGen.DungeonHalls += PreventAbyssDungeonInteractions_IL;

        public static void PreventAbyssDungeonInteractions_IL(ILContext context)
        {
            if (!InfernumConfig.Instance.CustomAbyssGeneration)
                return;
            // Prevent the Dungeon's halls from getting anywhere near the Abyss.
            var cursor = new ILCursor(context);

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
                unclampedValue.X = Clamp(unclampedValue.X, CustomAbyss.MaxAbyssWidth + 1000, Main.maxTilesX - CustomAbyss.MaxAbyssWidth - 1000);
                return unclampedValue;
            });
            cursor.Emit(OpCodes.Stloc, 6);
        }
    }

    internal sealed class ChangeBRSkyColorHook : ModSystem
    {
        public static MethodInfo? BRSkyColor = typeof(BossRushSky).GetMethod("get_GeneralColor", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeBRSkyColor_IL_Hook;

        public override void OnModLoad()
        {
            if (BRSkyColor != null)
            {
                ChangeBRSkyColor_IL_Hook = new(BRSkyColor, ChangeBRSkyColor_IL);
                ChangeBRSkyColor_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeBRSkyColor_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            cursor.EmitDelegate(() =>
            {
                Color color = Color.Lerp(new Color(205, 100, 100), Color.Black, WhiteDimness) * 0.2f;
                return color;
            });
            cursor.Emit(OpCodes.Ret);
        }
    }

    internal sealed class ChangeBREyeTextureHook : ModSystem
    {
        public static MethodInfo? BRXerocEyeTexure = typeof(BossRushSky).GetMethod("Draw", Utilities.UniversalBindingFlags);
        public static ILHook? ChangeBREyeTexture_IL_Hook;

        public override void OnModLoad()
        {
            if (BRXerocEyeTexure != null)
            {
                ChangeBREyeTexture_IL_Hook = new(BRXerocEyeTexure, ChangeBREyeTexture_IL);
                ChangeBREyeTexture_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void ChangeBREyeTexture_IL(ILContext context)
        {
            // Better to rewrite the entire thing to get it looking just right.
            ILCursor cursor = new(context);
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

    internal sealed class DeleteStupidScreenShadersHook : ModSystem
    {
        public static MethodInfo? SCalBackgroundSceneSpecialVisualsMethod = typeof(SCalBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags);
        public delegate void Orig_SCalBackgroundScene_SpecialVisuals(SCalBackgroundScene self, Player player, bool isActive);
        public static Hook? ChangeSCalSkyRequirements_Detour_Hook;

        public static MethodInfo? CalamitasCloneBackgroundSceneSpecialVisualsMethod = typeof(CalamitasCloneBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags);
        public delegate void Orig_CalamitasCloneBackgroundScene_SpecialVisuals(CalamitasCloneBackgroundScene self, Player player, bool isActive);
        public static Hook? ChangeCalCloneSkyRequirements_Detour_Hook;

        public static MethodInfo? YharonBackgroundSceneSpecialVisualsMethod = typeof(YharonBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags);
        public delegate void Orig_YharonBackgroundScene_SpecialVisuals(YharonBackgroundScene self, Player player, bool isActive);
        public static Hook? ChangeYharonSkyRequirements_Detour_Hook;
        public override void Load() => On_FilterManager.CanCapture += NoScreenShader;

        public override void OnModLoad()
        {
            if (SCalBackgroundSceneSpecialVisualsMethod != null)
            {
                ChangeSCalSkyRequirements_Detour_Hook = new(SCalBackgroundSceneSpecialVisualsMethod, ChangeSCalSkyRequirements);
                ChangeSCalSkyRequirements_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalamitasCloneBackgroundSceneSpecialVisualsMethod != null)
            {
                ChangeCalCloneSkyRequirements_Detour_Hook = new(CalamitasCloneBackgroundSceneSpecialVisualsMethod, ChangeCalCloneSkyRequirements);
                ChangeCalCloneSkyRequirements_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (YharonBackgroundSceneSpecialVisualsMethod != null)
            {
                ChangeSCalSkyRequirements_Detour_Hook = new(YharonBackgroundSceneSpecialVisualsMethod, ChangeYharonSkyRequirements);
                ChangeYharonSkyRequirements_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }
        private bool NoScreenShader(On_FilterManager.orig_CanCapture orig, FilterManager self)
        {
            if (CosmicBackgroundSystem.EffectIsActive)
                return false;

            return orig(self);
        }

        private void ChangeSCalSkyRequirements(Orig_SCalBackgroundScene_SpecialVisuals orig, SCalBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs)
                return;

            orig(instance, player, isActive);
        }

        private void ChangeCalCloneSkyRequirements(Orig_CalamitasCloneBackgroundScene_SpecialVisuals orig, CalamitasCloneBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs)
                return;

            orig(instance, player, isActive);
        }

        private void ChangeYharonSkyRequirements(Orig_YharonBackgroundScene_SpecialVisuals orig, YharonBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs && !InfernumConfig.Instance.ReducedGraphicsConfig)
                return;

            orig(instance, player, isActive);
        }
    }

    internal sealed class AdjustASWaterPoisonTimersHook : ModSystem
    {
        public static MethodInfo? UpdateBadLifeRegen = typeof(CalamityPlayer).GetMethod("UpdateBadLifeRegen", Utilities.UniversalBindingFlags);
        public static ILHook? AdjustASWaterPoisonTimers_IL_Hook;

        public override void OnModLoad()
        {
            if (UpdateBadLifeRegen != null)
            {
                AdjustASWaterPoisonTimers_IL_Hook = new(UpdateBadLifeRegen, AdjustASWaterPoisonTimers_IL);
                AdjustASWaterPoisonTimers_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void AdjustASWaterPoisonTimers_IL(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.EmitDelegate(() =>
            {
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

    internal sealed class MakeDungeonSpawnAtLeftSideHook : ModSystem
    {
        // This is so hideous but the alternative is IL editing on anonymous methods.
        internal static bool ReturnZeroInRandomness;

        public override void Load()
        {
            On_UnifiedRandom.Next_int += HijackRNG;

            On_WorldGen.RandomizeMoonState += PrepareDungeonSide;
        }

        public static void PrepareDungeonSide(On_WorldGen.orig_RandomizeMoonState orig, UnifiedRandom random, bool garenteeNewStyle)
        {
            orig(random, garenteeNewStyle);
            ReturnZeroInRandomness = true;
        }

        public int HijackRNG(On_UnifiedRandom.orig_Next_int orig, UnifiedRandom self, int maxValue)
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

    internal sealed class DrawNightStarsHook : ModSystem
    {
        public override void Load() => On_Main.DrawStarsInBackground += DrawStarsHook;

        private void DrawStarsHook(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
        {
            // Do not draw if the flower ocean visuals are active.
            if (!Main.gameMenu && Main.LocalPlayer.Infernum().GetValue<bool>("FlowerOceanVisualsActive"))
                return;

            orig(self, sceneArea, artificial);
        }
    }

    internal sealed class DisableWaterDrawingDuringAEWHook : ModSystem
    {
        public override void Load() => On_Main.DrawWaters += DisableWaterDrawing;

        private void DisableWaterDrawing(On_Main.orig_DrawWaters orig, Main self, bool isBackground)
        {
            if (InfernumMode.CanUseCustomAIs && Main.LocalPlayer.Calamity().ZoneAbyssLayer4)
                return;

            orig(self, isBackground);
        }
    }

    internal sealed class ChangeCalCloneNameHook : ModSystem
    {
        public override void Load() => On_NPC.DoDeathEvents_DropBossPotionsAndHearts += ChangeName;

        private void ChangeName(On_NPC.orig_DoDeathEvents_DropBossPotionsAndHearts orig, NPC npc, ref string typeName)
        {
            orig(npc, ref typeName);
            if (npc.type == ModContent.NPCType<CalamitasClone>() && InfernumMode.CanUseCustomAIs)
                typeName = Utilities.GetLocalization("NameOverrides.CalamitasShadowClone.EntryName").Format(CalamitasShadowBehaviorOverride.CustomName);
        }
    }

    internal sealed class MakeEternityOPHook : ModSystem
    {
        public static MethodInfo? EternityHexAI = typeof(EternityHex).GetMethod("AI", Utilities.UniversalBindingFlags);
        public static ILHook? MakeEternityOP_IL_Hook;

        public override void OnModLoad()
        {
            if (EternityHexAI != null)
            {
                MakeEternityOP_IL_Hook = new(EternityHexAI, MakeEternityOP_IL);
                MakeEternityOP_IL_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");
        }

        public static void MakeEternityOP_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            while (cursor.TryGotoNext(i => i.MatchCallOrCallvirt<StatModifier>("ApplyTo")))
            {
                cursor.GotoNext(MoveType.Before, i => i.MatchLdcR4(out float originalDamage));
                cursor.Remove();
                cursor.EmitDelegate(() => (float)ItemDamageValues.DamageValues[ModContent.ItemType<Eternity>()]);
            }
        }
    }

    internal sealed class AntiFlashbangSupportHook : ModSystem
    {
        public override void Load() => IL_MoonlordDeathDrama.DrawWhite += AntiFlashbangSupport_IL;

        // Do NOT remove this method, despite its 0 references it is used by IL.
        public static Color DrawColor() => InfernumConfig.Instance.FlashbangOverlays ? Color.White : Color.Black;

        public static void AntiFlashbangSupport_IL(ILContext context)
        {
            ILCursor cursor = new(context);

            // Replace the white color with a black one, if the flashbang config is disbaled.
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchCall<Color>("get_White")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.EmitCall(typeof(AntiFlashbangSupportHook).GetMethod("DrawColor", Utilities.UniversalBindingFlags)!);
            }
        }
    }
}
