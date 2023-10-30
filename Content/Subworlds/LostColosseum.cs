using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Schematics;
using CalamityMod.World;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Achievements.InfernumAchievements;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.Content.Subworlds
{
    public class LostColosseum : Subworld
    {
        public class LostColosseumGenPass : GenPass
        {
            public LostColosseumGenPass() : base("Terrain", 1f) { }

            protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
            {
                progress.Message = "Generating a Lost Colosseum";
                Main.worldSurface = Main.maxTilesY - 25;
                Main.rockLayer = Main.maxTilesY - 30;

                for (int i = 0; i < CaveWidth; i++)
                {
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        Main.tile[i, j].TileType = TileID.Sandstone;
                        Main.tile[i, j].Get<TileWallWireStateData>().HasTile = true;
                    }
                }

                bool _ = false;
                Point bottomLeftOfWorld = new(Main.maxTilesX - 37, Main.maxTilesY - 31);
                PlaceSchematic<Action<Chest>>("LostColosseum", bottomLeftOfWorld, SchematicAnchor.BottomRight, ref _);

                for (int i = 0; i < CaveWidth + 188; i++)
                {
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        if (j < Main.maxTilesY - 120 && i >= CaveWidth + 165)
                            continue;

                        Main.tile[i, j].WallType = WallID.Sandstone;
                    }
                }

                // Set the default spawn position.
                Main.spawnTileX = PortalPosition.X + 12;
                Main.spawnTileY = PortalPosition.Y;

                // Spawn the actual entrance because re-exporting the entire schematic fucks up the rest of the worldgen code here.
                Point exitCenter = new(PortalPosition.X, PortalPosition.Y + 8);
                for (int dx = -51; dx < 51; dx++)
                {
                    for (int dy = 0; dy < 45; dy++)
                    {
                        Main.tile[exitCenter.X + dx, exitCenter.Y - dy].TileType = TileID.Sandstone;
                        Main.tile[exitCenter.X + dx, exitCenter.Y - dy].Get<TileWallWireStateData>().HasTile = true;
                    }
                }
                PlaceSchematic<Action<Chest>>("LostColosseumExit", exitCenter, SchematicAnchor.Center, ref _);

                // Why the fuck???
                for (int x = 586; x < 592; x++)
                {
                    for (int y = 140; y < 146; y++)
                    {
                        Main.tile[x, y].TileType = TileID.Sand;
                        Main.tile[x, y].Get<TileWallWireStateData>().IsActuated = false;
                        Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                    }
                }
                for (int x = 695; x < 702; x++)
                {
                    for (int y = 160; y < 190; y++)
                    {
                        Main.tile[x, y].TileType = TileID.SandstoneBrick;
                        Main.tile[x, y].Get<TileWallWireStateData>().IsActuated = false;
                        Main.tile[x, y].Get<TileWallWireStateData>().TileColor = PaintID.ShadowPaint;
                        Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                    }
                }

                // Ensure that the portal is open when the player is there.
                WorldSaveSystem.HasOpenedLostColosseumPortal = true;
            }

            public static void GenerateCaveSystem(Point start, Point end)
            {
                Point midpoint = new(CaveWidth / -4, (start.Y + end.Y) / 2 + WorldGen.genRand.Next(-25, 25));
                List<Vector2> baseCurvePoints = new BezierCurve(start.ToVector2(), midpoint.ToVector2(), end.ToVector2()).GetPoints(108);

                // Add a bit of direction variance based on perlin noise to the points.
                int caveSeed = WorldGen.genRand.Next();
                for (int i = 0; i < baseCurvePoints.Count; i++)
                {
                    Vector2 perpendicularDirection = Vector2.UnitY;
                    float noiseOffset = SulphurousSea.FractalBrownianMotion(baseCurvePoints[i].X / 120f, baseCurvePoints[i].Y / 120f, caveSeed, 3) * 28f;
                    if (i >= 1)
                        perpendicularDirection = (baseCurvePoints[i] - baseCurvePoints[i - 1]).SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2);

                    baseCurvePoints[i] += perpendicularDirection * noiseOffset;
                    if (baseCurvePoints[i].X <= 25f)
                        baseCurvePoints[i] = new Vector2(25f, baseCurvePoints[i].Y);
                }

                // Carve out caves.
                foreach (Vector2 curvePoint in baseCurvePoints)
                {
                    WorldUtils.Gen(curvePoint.ToPoint(), new Shapes.Circle(8, 13), Actions.Chain(new GenAction[]
                    {
                        new Modifiers.Blotches(3, 0.27),
                        new Actions.ClearTile()
                    }));
                }
            }
        }

        internal static bool VassalWasBeaten;

        internal static bool WasInColosseumLastFrame;

        public static bool HasBereftVassalAppeared
        {
            get;
            set;
        }

        public static bool HasBereftVassalBeenDefeated
        {
            get;
            set;
        }

        public static float SunsetInterpolant
        {
            get;
            set;
        }

        public static Color SunlightColor =>
            Color.Lerp(Color.White, new(210, 85, 135), SunsetInterpolant * SunsetInterpolant * 0.67f);

        public static int SchematicWidth => 1435;

        public static int SchematicHeight => 203;

        public static int CaveWidth => 280;

        public static Point PortalPosition => new(CaveWidth + 166, 155);

        public static Point CampfirePosition => new(CaveWidth + 464, 160);

        public override int Width => SchematicWidth + CaveWidth - 64;

        public override int Height => SchematicHeight + 32;

        public override bool ShouldSave => true;

        public override List<GenPass> Tasks => new()
        {
            new LostColosseumGenPass()
        };

        public static Texture2D LoadingBackgroundTexture
        {
            get;
            private set;
        }

        public static Texture2D LoadingAnimationTexture
        {
            get;
            private set;
        }

        public static int FrameCounter
        {
            get;
            set;
        }

        public static int Frame
        {
            get;
            set;
        }

        public override void Load()
        {
            LoadingBackgroundTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Subworlds/ColosseumLoadingBackground", AssetRequestMode.ImmediateLoad).Value;
            LoadingAnimationTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Subworlds/LoadingAnimation", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            LoadingBackgroundTexture = null;
            LoadingAnimationTexture = null;
        }

        public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
        {
            Vector3 lightMin = Vector3.Zero;
            bool notSolid = tile.Slope != SlopeType.Solid || tile.IsHalfBlock;
            if (!tile.HasTile || !Main.tileNoSunLight[tile.TileType] || notSolid && Main.wallLight[tile.WallType] && tile.LiquidAmount < 200)
                lightMin = SunlightColor.ToVector3();

            color = Vector3.Max(color, lightMin);
            return false;
        }

        public override void OnExit()
        {
            // Reset the sunset interpolant.
            SunsetInterpolant = 0f;

            // Ensure that the vassal defeat achievement translates over when the player goes to a different subworld.
            if (Main.netMode != NetmodeID.Server)
            {
                foreach (var achievement in Main.LocalPlayer.GetModPlayer<AchievementPlayer>().AchievementInstances)
                {
                    if (achievement.GetType() == typeof(BereftVassalAchievement))
                    {
                        if (achievement.DoneCompletionEffects)
                            VassalWasBeaten = true;
                    }
                }
            }

            // Ensure that the player returns to their original spot before entering the subworld instead of to their typical spawn point.
            Main.LocalPlayer.Infernum_Biome().ReturnToPositionBeforeSubworld = true;
        }

        public static void ManageSandstorm()
        {
            Main.windSpeedCurrent = 1.5f;
            bool useSandstorm = !HasBereftVassalBeenDefeated;
            int vassal = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());
            if (useSandstorm && vassal >= 0 && !Main.LocalPlayer.dead)
                useSandstorm = Main.npc[vassal].ModNPC<BereftVassal>().CurrentAttack == BereftVassal.BereftVassalAttackType.IdleState;

            if (useSandstorm)
            {
                Sandstorm.Happening = true;
                Sandstorm.TimeLeft = 240;
                Sandstorm.IntendedSeverity = Sandstorm.Severity = 1.5f;
            }
            else
            {
                Sandstorm.StopSandstorm();
                Sandstorm.Severity *= 0.96f;
            }
        }

        public static void UpdateSunset()
        {
            // 12:00 PM.
            int noon = 27000;

            // 5:00 PM.
            int evening = noon + 18000;
            Main.time = (int)Lerp(noon, evening, SunsetInterpolant);

            if (HasBereftVassalBeenDefeated)
                SunsetInterpolant = 1f;
        }

        public override void DrawMenu(GameTime gameTime)
        {
            Vector2 drawOffset = Vector2.Zero;
            float xScale = (float)Main.screenWidth / LoadingBackgroundTexture.Width;
            float yScale = (float)Main.screenHeight / LoadingBackgroundTexture.Height;
            float scale = xScale;

            if (xScale != yScale)
            {
                if (yScale > xScale)
                {
                    scale = yScale;
                    drawOffset.X -= (LoadingBackgroundTexture.Width * scale - Main.screenWidth) * 0.5f;
                }
                else
                    drawOffset.Y -= (LoadingBackgroundTexture.Height * scale - Main.screenHeight) * 0.5f;
            }

            Effect sandstorm = InfernumEffectsRegistry.SandstormShader.GetShader().Shader;
            sandstorm.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            sandstorm.Parameters["intensity"].SetValue(0.6f);
            sandstorm.Parameters["distortAmount"].SetValue(0.0075f);
            sandstorm.Parameters["distortZoom"].SetValue(5.2f);
            sandstorm.Parameters["distortSpeed"].SetValue(0.2f);
            sandstorm.Parameters["sandZoom"].SetValue(1.5f);
            sandstorm.Parameters["lerpIntensity"].SetValue(0.5f);

            sandstorm.Parameters["resolution"].SetValue(Utilities.CreatePixelationResolution(new Vector2(Main.screenWidth, Main.screenHeight)));
            sandstorm.Parameters["mainColor"].SetValue(new Color(0.74f, 0.56f, 0.4f).ToVector3());

            Utilities.SetTexture1(InfernumTextureRegistry.BlurryPerlinNoise.Value);
            Utilities.SetTexture2(InfernumTextureRegistry.HarshNoise.Value);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, Main.Rasterizer, sandstorm, Main.UIScaleMatrix);

            Main.spriteBatch.Draw(LoadingBackgroundTexture, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            Vector2 textPosition = new Vector2(Main.screenWidth, Main.screenHeight) / 2f - FontAssets.DeathText.Value.MeasureString(Main.statusText) / 2f;

            Main.spriteBatch.DrawString(FontAssets.DeathText.Value, Main.statusText, textPosition, Color.White);

            float sineOffset = (1f + Sin(Pi * Main.GlobalTimeWrappedHourly)) * 0.5f;
            Vector2 animationDrawPosition = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + Vector2.UnitY * Lerp(-100f, -125f, sineOffset) + Vector2.UnitX * -20f;

            int frameCount = 8;
            int frameHeight = LoadingAnimationTexture.Height / frameCount;

            FrameCounter++;
            if (FrameCounter > 6)
            {
                Frame++;
                if (Frame > 7)
                    Frame = 0;
                FrameCounter = 0;
            }

            Rectangle frame = new(0, frameHeight * Frame, LoadingAnimationTexture.Width, frameHeight);
            Main.spriteBatch.Draw(LoadingAnimationTexture, animationDrawPosition, frame, Color.White, 0f, frame.Size() * 0.5f, 1f, SpriteEffects.FlipHorizontally, 0f);
        }
    }
}
