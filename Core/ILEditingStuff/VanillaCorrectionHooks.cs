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
using CalamityMod.Projectiles.Enemy;
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
using InfernumMode.Core.GlobalInstances.Systems;
using Luminance.Core.Hooking;
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
using Terraria.GameContent.UI.States;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static CalamityMod.Events.BossRushEvent;
using static InfernumMode.Core.ILEditingStuff.HookManager;
using InfernumBalancingManager = InfernumMode.Core.Balancing.BalancingChangesManager;

namespace InfernumMode.Core.ILEditingStuff
{
    public class ReplaceGoresHook : IExistingDetourProvider
    {
        internal static List<int> InvalidGoreIDs =
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

        void IExistingDetourProvider.Subscribe() => On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += AlterGores;

        void IExistingDetourProvider.Unsubscribe() => On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float -= AlterGores;

        private static int AlterGores(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
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
    }

    public class MoveDraedonHellLabHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => PlaceHellLab += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => PlaceHellLab -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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

    public class MakeCheeseSulphSeaCavesBiggerHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => GenerateSulphSeaCheeseCaves += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => GenerateSulphSeaCheeseCaves -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class MakeSpaghettiSulphSeaCavesBiggerHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => GenerateSulphSeaSpaghettiCaves += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => GenerateSulphSeaSpaghettiCaves -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class ManipulateSunPositionHook : ILEditProvider, IExistingDetourProvider
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

        public override void Subscribe(ManagedILEdit edit) => IL_Main.DoDraw += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_Main.DoDraw -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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

        void IExistingDetourProvider.Subscribe()
        {
            On_Main.UpdateAtmosphereTransparencyToSkyColor += GetRidOfPeskyBlackSpaceFade;
            On_Main.DrawSunAndMoon += DrawStrongerSunInColosseum;
            On_Main.DrawBlack += ForceDrawBlack;
        }

        void IExistingDetourProvider.Unsubscribe()
        {
            On_Main.UpdateAtmosphereTransparencyToSkyColor -= GetRidOfPeskyBlackSpaceFade;
            On_Main.DrawSunAndMoon -= DrawStrongerSunInColosseum;
            On_Main.DrawBlack -= ForceDrawBlack;
        }

        internal void ForceDrawBlack(On_Main.orig_DrawBlack orig, Main self, bool force)
        {
            orig(self, force || LostColosseum.WasInColosseumLastFrame || CeaselessDimensionDrawSystem.BackgroundChangeInterpolant > 0f);
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

    public class ChangeDrawBlackLimitHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_Main.DrawBlack += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_Main.DrawBlack -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class GetRidOfYharonOnHitDebuffsHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => YharonOnHitPlayer += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => YharonOnHitPlayer -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit) => HookHelper.EarlyReturnEdit(il, edit);
    }

    public class GetRidOfSCalOnHitDebuffsHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => SCalOnHitPlayer += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => SCalOnHitPlayer -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit) => HookHelper.EarlyReturnEdit(il, edit);
    }

    public class ChangeBossRushTiersHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => BossRushTier += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => BossRushTier -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class ChangeExoMechBackgroundColorHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => ExoMechTileTileColor += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => ExoMechTileTileColor -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class DisableExoMechsSkyInBRHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => ExoMechsSkyIsActive += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => ExoMechsSkyIsActive -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class GetRidOfProvidenceLootBoxHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => SpawnProvLootBox += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => SpawnProvLootBox -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit) => HookHelper.EarlyReturnEdit(il, edit);
    }

    public class AddWarningAboutNonExpertOnWorldSelectionHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_UIWorldCreation.AddWorldDifficultyOptions += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_UIWorldCreation.AddWorldDifficultyOptions -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class ReducePlayerDashDelay : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => DashMovement += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => DashMovement -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class AureusPlatformWalkingHook : IExistingDetourProvider
    {
        void IExistingDetourProvider.Subscribe() => On_NPC.Collision_DecideFallThroughPlatforms += LetAureusWalkOnPlatforms;

        void IExistingDetourProvider.Unsubscribe() => On_NPC.Collision_DecideFallThroughPlatforms -= LetAureusWalkOnPlatforms;

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

    public class FishronSkyDistanceLeniancyHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_ScreenDarkness.Update += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_ScreenDarkness.Update -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchLdcR4(3000f));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_R4, 6000f);
        }
    }

    public class EyeOfCthulhuSpawnHPMinChangeHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_Main.UpdateTime_StartNight += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_Main.UpdateTime_StartNight -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchLdcI4(200));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4, 400);
        }
    }

    public class KingSlimeSpawnHPMinChangeHook : ILEditProvider, IExistingDetourProvider
    {
        private static bool spawningKingSlimeNaturally;

        public override void Subscribe(ManagedILEdit edit) => IL_NPC.SpawnNPC += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_NPC.SpawnNPC -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(i => i.MatchCall<NPC>("SpawnOnPlayer"));
            cursor.EmitDelegate<Action>(() => spawningKingSlimeNaturally = true);
        }

        void IExistingDetourProvider.Subscribe() => On_NPC.SpawnOnPlayer += OptionallyDisableKSSpawn;

        void IExistingDetourProvider.Unsubscribe() => On_NPC.SpawnOnPlayer += OptionallyDisableKSSpawn;

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

    public class UseCustomShineParticlesForInfernumParticlesHook : IExistingDetourProvider
    {
        void IExistingDetourProvider.Subscribe() => On_TileDrawing.DrawTiles_EmitParticles += EmitFireParticles;

        void IExistingDetourProvider.Unsubscribe() => On_TileDrawing.DrawTiles_EmitParticles -= EmitFireParticles;

        private static void EmitFireParticles(On_TileDrawing.orig_DrawTiles_EmitParticles orig, TileDrawing self, int j, int i, Tile tileCache, ushort typeCache, short tileFrameX, short tileFrameY, Color tileLight)
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
    }

    public class ReplaceAbyssWorldgen : ICustomDetourProvider
    {
        void ICustomDetourProvider.ModifyMethods() => HookHelper.ModifyMethodWithDetour(typeof(Abyss).GetMethod("PlaceAbyss", Utilities.UniversalBindingFlags), ChangeAbyssGen);

        internal static void ChangeAbyssGen(Action orig) => CustomAbyss.Generate();
    }

    public class GetRidOfDesertNuisancesHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => DesertScourgeItemUseItem += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => DesertScourgeItemUseItem -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
    }

    public class LetAresHitPlayersHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => AresBodyCanHitPlayer += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => AresBodyCanHitPlayer -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }
    }

    public class AdjustAbyssDefinitionHook : ICustomDetourProvider
    {
        void ICustomDetourProvider.ModifyMethods()
        {
            HookHelper.ModifyMethodWithDetour(typeof(AbyssLayer1Biome).GetMethod("MeetsBaseAbyssRequirement", Utilities.UniversalBindingFlags), ChangeAbyssRequirement);
            HookHelper.ModifyMethodWithDetour(typeof(AbyssLayer1Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), ChangeLayer1Requirement);
            HookHelper.ModifyMethodWithDetour(typeof(AbyssLayer2Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), ChangeLayer2Requirement);
            HookHelper.ModifyMethodWithDetour(typeof(AbyssLayer3Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), ChangeLayer3Requirement);
            HookHelper.ModifyMethodWithDetour(typeof(AbyssLayer4Biome).GetMethod("IsBiomeActive", Utilities.UniversalBindingFlags), ChangeLayer4Requirement);

        }
        private bool ChangeAbyssRequirement(AbyssRequirementDelegate orig, Player player, out int playerYTileCoords)
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

        private bool ChangeLayer1Requirement(Func<AbyssLayer1Biome, Player, bool> orig, AbyssLayer1Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords <= CustomAbyss.Layer2Top;
            }

            return orig(self, player);
        }

        private bool ChangeLayer2Requirement(Func<AbyssLayer2Biome, Player, bool> orig, AbyssLayer2Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer2Top && playerYTileCoords <= CustomAbyss.Layer3Top;
            }

            return orig(self, player);
        }

        private bool ChangeLayer3Requirement(Func<AbyssLayer3Biome, Player, bool> orig, AbyssLayer3Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer3Top && playerYTileCoords <= CustomAbyss.Layer4Top;
            }

            return orig(self, player);
        }

        private bool ChangeLayer4Requirement(Func<AbyssLayer4Biome, Player, bool> orig, AbyssLayer4Biome self, Player player)
        {
            if (WorldSaveSystem.InPostAEWUpdateWorld)
            {
                return AbyssLayer1Biome.MeetsBaseAbyssRequirement(player, out int playerYTileCoords) &&
                    playerYTileCoords > CustomAbyss.Layer4Top && playerYTileCoords <= CustomAbyss.AbyssBottom;
            }

            return orig(self, player);
        }
    }

    public class MakeMapGlitchInLayer4AbyssHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_Main.DrawMap += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_Main.DrawMap -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);
            MethodInfo colorFloatMultiply = typeof(Color).GetMethod("op_Multiply", [typeof(Color), typeof(float)]);
            ConstructorInfo colorConstructor = typeof(Color).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(int)]);

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

    public class PreventAbyssDungeonInteractionsHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_WorldGen.DungeonHalls += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_WorldGen.DungeonHalls -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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
                unclampedValue.X = Clamp(unclampedValue.X, CustomAbyss.MaxAbyssWidth + 1000, Main.maxTilesX - CustomAbyss.MaxAbyssWidth - 1000);
                return unclampedValue;
            });
            cursor.Emit(OpCodes.Stloc, 6);
        }
    }

    public class ChangeBRSkyColorHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => BRSkyColor += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => BRSkyColor -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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

    public class ChangeBREyeTextureHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => BRXerocEyeTexure += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => BRXerocEyeTexure -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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

    public class DeleteStupidScreenShadersHook : IExistingDetourProvider, ICustomDetourProvider
    {
        // This is unideal, should probably add a better method of adding existing and custom detours together, but its not high priority.
        // This is the only one thats needed it in our excessive hooks, so it's mostly fine.
        void ILoadable.Load(Mod mod)
        {
            Subscribe();
            ModifyMethods();
        }

        void ILoadable.Unload() { }

        public void Subscribe() => On_FilterManager.CanCapture += NoScreenShader;

        void IExistingDetourProvider.Unsubscribe() => On_FilterManager.CanCapture -= NoScreenShader;

        private bool NoScreenShader(On_FilterManager.orig_CanCapture orig, FilterManager self)
        {
            if (CosmicBackgroundSystem.EffectIsActive)
                return false;

            return orig(self);
        }

        public void ModifyMethods()
        {
            HookHelper.ModifyMethodWithDetour(typeof(SCalBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), ChangeSCalSkyRequirements);
            HookHelper.ModifyMethodWithDetour(typeof(CalamitasCloneBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), ChangeCalCloneSkyRequirements);
            HookHelper.ModifyMethodWithDetour(typeof(YharonBackgroundScene).GetMethod("SpecialVisuals", Utilities.UniversalBindingFlags), ChangeYharonSkyRequirements);

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

        private void ChangeYharonSkyRequirements(Action<YharonBackgroundScene, Player, bool> orig, YharonBackgroundScene instance, Player player, bool isActive)
        {
            if (InfernumMode.CanUseCustomAIs && !InfernumConfig.Instance.ReducedGraphicsConfig)
                return;

            orig(instance, player, isActive);
        }
    }

    public class AdjustASWaterPoisonTimersHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => UpdateBadLifeRegen += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => UpdateBadLifeRegen -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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

    public class MakeDungeonSpawnAtLeftSideHook : IExistingDetourProvider
    {
        // This is so hideous but the alternative is IL editing on anonymous methods.
        internal static bool ReturnZeroInRandomness;

        void IExistingDetourProvider.Subscribe()
        {
            On_WorldGen.RandomizeMoonState += PrepareDungeonSide;
            On_UnifiedRandom.Next_int += HijackRNG;

        }

        void IExistingDetourProvider.Unsubscribe()
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

    public class DrawNightStarsHook : IExistingDetourProvider
    {
        void IExistingDetourProvider.Subscribe() => On_Main.DrawStarsInBackground += DrawStarsHook;

        void IExistingDetourProvider.Unsubscribe() => On_Main.DrawStarsInBackground -= DrawStarsHook;

        private void DrawStarsHook(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
        {
            // Do not draw if the flower ocean visuals are active.
            if (!Main.gameMenu && Main.LocalPlayer.Infernum().GetValue<bool>("FlowerOceanVisualsActive"))
                return;

            orig(self, sceneArea, artificial);
        }
    }

    public class DisableWaterDrawingDuringAEWHook : IExistingDetourProvider
    {
        void IExistingDetourProvider.Subscribe() => On_Main.DrawWaters += DisableWaterDrawing;

        void IExistingDetourProvider.Unsubscribe() => On_Main.DrawWaters -= DisableWaterDrawing;

        private void DisableWaterDrawing(On_Main.orig_DrawWaters orig, Main self, bool isBackground)
        {
            if (InfernumMode.CanUseCustomAIs && Main.LocalPlayer.Calamity().ZoneAbyssLayer4)
                return;

            orig(self, isBackground);
        }
    }

    public class ChangeCalCloneNameHook : IExistingDetourProvider
    {
        void IExistingDetourProvider.Subscribe() => On_NPC.DoDeathEvents_DropBossPotionsAndHearts += ChangeName;

        void IExistingDetourProvider.Unsubscribe() => On_NPC.DoDeathEvents_DropBossPotionsAndHearts -= ChangeName;

        private void ChangeName(On_NPC.orig_DoDeathEvents_DropBossPotionsAndHearts orig, NPC npc, ref string typeName)
        {
            orig(npc, ref typeName);
            if (npc.type == ModContent.NPCType<CalamitasClone>() && InfernumMode.CanUseCustomAIs)
                typeName = Utilities.GetLocalization("NameOverrides.CalamitasShadowClone.EntryName").Format(CalamitasShadowBehaviorOverride.CustomName);
        }
    }

    public class MakeEternityOPHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => EternityHexAI += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => EternityHexAI -= edit.SubscriptionWrapper;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
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

    public class AntiFlashbangSupportHook : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit) => IL_MoonlordDeathDrama.DrawWhite += edit.SubscriptionWrapper;

        public override void Unsubscribe(ManagedILEdit edit) => IL_MoonlordDeathDrama.DrawWhite -= edit.SubscriptionWrapper;

        // Do NOT remove this method, despite its 0 references it is used by IL.
        public static Color DrawColor() => InfernumConfig.Instance.FlashbangOverlays ? Color.White : Color.Black;

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);

            // Replace the white color with a black one, if the flashbang config is disbaled.
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchCall<Color>("get_White")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.EmitCall(typeof(AntiFlashbangSupportHook).GetMethod("DrawColor", Utilities.UniversalBindingFlags));
            }
        }
    }

    public class VanillaProjectileImmunitySlotHook : IExistingDetourProvider
    {
        public static List<int> VanillaBossProjectiles
        {
            get;
            private set;
        }

        // IDK why she uses normal stingers for some atttacks but I'd rather do this than change it now.
        public static readonly Dictionary<int, Func<bool>> VanillaProjectilesWithConditions = new()
        {
            [ProjectileID.Stinger] = () => { return NPC.AnyNPCs(NPCID.QueenBee); }
        };

        public static bool ConvertNextNonPVPHurtImmuneSlot
        {
            get;
            private set;
        }

        void IExistingDetourProvider.Subscribe()
        {
            On_Projectile.Damage += CheckProjectile;
            On_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float += HurtDetour;
        }

        void IExistingDetourProvider.Unsubscribe()
        {
            On_Projectile.Damage -= CheckProjectile;
            On_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float -= HurtDetour;
        }

        private double HurtDetour(On_Player.orig_Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float orig, Player self, PlayerDeathReason damageSource, int Damage, int hitDirection, out Player.HurtInfo info, bool pvp, bool quiet, int cooldownCounter, bool dodgeable, float armorPenetration, float scalingArmorPenetration, float knockback)
        {
            // If marked as ready, convert the cooldown slot to be the correct one.
            if (!pvp && ConvertNextNonPVPHurtImmuneSlot)
                cooldownCounter = ImmunityCooldownID.Bosses;

            return orig(self, damageSource, Damage, hitDirection, out info, pvp, quiet, cooldownCounter, dodgeable, armorPenetration, scalingArmorPenetration, knockback);
        }

        private void CheckProjectile(On_Projectile.orig_Damage orig, Projectile self)
        {
            // Initialize the list. Can't be done in the initializer due to the mod projectile.
            VanillaBossProjectiles ??=
            [
                ProjectileID.QueenBeeStinger,
                ProjectileID.DeathLaser,
                ProjectileID.EyeLaser,
                ProjectileID.Skull,
                ProjectileID.SeedPlantera,
                ProjectileID.PoisonSeedPlantera,
                ProjectileID.MartianTurretBolt,
                // It shouldn't really be using this but Giant Clam has a lot worse issues than this.
                ModContent.ProjectileType<PearlRain>()
            ];

            // If the current projectile is in the list, mark the conversion as ready.
            if (VanillaBossProjectiles.Contains(self.type) || (VanillaProjectilesWithConditions.TryGetValue(self.type, out var condition) && condition()))
                ConvertNextNonPVPHurtImmuneSlot = true;

            // The hurt detour is ran in orig, so restore it to false after it has ran.
            orig(self);
            ConvertNextNonPVPHurtImmuneSlot = false;
        }
    }
}
