using CalamityMod;
using CalamityMod.Items.SummonItems;
using CalamityMod.Tiles.Astral;
using InfernumMode.Graphics;
using InfernumMode.Projectiles;
using InfernumMode.Subworlds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Tiles
{
    public class ColosseumPortal : ModTile
    {
        public const int Width = 5;

        public const int Height = 2;

        public static float AnimationCompletion => WorldSaveSystem.LostColosseumPortalAnimationTimer / (float)WorldSaveSystem.LostColosseumPortalAnimationTime;
        
        public static float PortalWidth => Width * 16f + 32f;

        public static PrimitiveTrailCopy SandPillarDrawer
        {
            get;
            set;
        } = null;

        public PrimitiveTrailCopy PortalDrawer
        {
            get;
            set;
        } = null;

        internal static List<Point> PortalCache = new();

        public override void SetStaticDefaults()
        {
            MinPick = int.MaxValue;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            // This is necessary to ensure that the primitives properly render.
            TileID.Sets.DrawTileInSolidLayer[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(3, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(150, 96, 87));
        }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            if (SubworldSystem.IsActive<LostColosseum>())
                WorldSaveSystem.HasOpenedLostColosseumPortal = true;

            if (WorldSaveSystem.HasOpenedLostColosseumPortal)
                texture = ModContent.Request<Texture2D>("InfernumMode/Tiles/ColosseumPortalOpen").Value;

            Color color = Lighting.GetColor(i, j);
            Rectangle frame = new(t.TileFrameX, t.TileFrameY, 18, 18);
            Vector2 drawPosition = new Vector2(i * 16f, j * 16f) - Main.screenPosition + Vector2.UnitY * 2f;
            if (!Main.drawToScreen)
                drawPosition += Vector2.One * Main.offScreenRange;

            spriteBatch.Draw(texture, drawPosition, frame, color, 0f, Vector2.Zero, 1f, 0, 0f);

            // Draw the portal pillar.
            if (t.TileFrameX != 54 || t.TileFrameY != 18)
                return false;

            SandPillarDrawer ??= new PrimitiveTrailCopy(PillarWidthFunction, PillarColorFunction, null, true, InfernumEffectsRegistry.DarkFlamePillarVertexShader);

            Point p = new(i, j);
            if (PortalCache.Contains(p))
                PortalCache.Remove(p);

            if (WorldSaveSystem.HasOpenedLostColosseumPortal)
                PortalCache.Add(p);

            return false;
        }

        public override bool RightClick(int i, int j)
        {
            if (!Main.LocalPlayer.HasItem(ModContent.ItemType<SandstormsCore>()) && !WorldSaveSystem.HasOpenedLostColosseumPortal)
                return true;

            if (WorldSaveSystem.HasOpenedLostColosseumPortal)
            {
                if (SubworldSystem.IsActive<LostColosseum>())
                    SubworldSystem.Exit();
                else
                {
                    // Don't allow the player to use the portal if Infernum is not active.
                    if (!InfernumMode.CanUseCustomAIs)
                    {
                        CombatText.NewText(Main.LocalPlayer.Hitbox, Color.Orange, "Infernum must be enabled to enter the Colosseum!");
                        return true;
                    }

                    Main.LocalPlayer.Infernum_Biome().PositionBeforeEnteringSubworld = Main.LocalPlayer.Center;
                    SubworldSystem.Enter<LostColosseum>();
                }
                return true;
            }

            SoundEngine.PlaySound(AstralBeacon.UseSound);
            SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen);
            WorldSaveSystem.HasOpenedLostColosseumPortal = true;
            WorldSaveSystem.LostColosseumPortalAnimationTimer = 0;
            
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.WorldData);

            // Create a lens flare.
            Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);
            Vector2 lensFlareDrawPosition = new Point(i, j).ToWorldCoordinates() - new Vector2(t.TileFrameX, t.TileFrameY) + Vector2.UnitX * 36f;
            Projectile.NewProjectile(new EntitySource_WorldEvent(), lensFlareDrawPosition, Vector2.Zero, ModContent.ProjectileType<PortalLensFlare>(), 0, 0f);
            return true;
        }

        public static float PillarWidthFunction(float completionRatio)
        {
            float tipFadeoffInterpolant = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(1f, 0.75f, completionRatio, true));
            float baseFadeoffInterpolant = MathHelper.SmoothStep(2.4f, 1f, 1f - CalamityUtils.Convert01To010(Utils.GetLerpValue(0f, 0.64f, completionRatio, true)));
            float widthAdditionFactor = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -13f + completionRatio * MathHelper.Pi * 4f) * 0.2f;
            float generalSquishFactor = Utils.GetLerpValue(0.8f, 0.96f, AnimationCompletion, true);

            return tipFadeoffInterpolant * baseFadeoffInterpolant * (1f + widthAdditionFactor) * PortalWidth * generalSquishFactor;
        }

        public static Color PillarColorFunction(float completionRatio)
        {
            Color lightSandColor = new(234, 179, 112);
            float colorShiftInterpolant = (float)Math.Sin(-Main.GlobalTimeWrappedHourly * 6.7f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f;
            float opacity = Utils.GetLerpValue(0.84f, 0.96f, AnimationCompletion, true) * Utils.GetLerpValue(0f, 0.13f, completionRatio, true);
            return Color.Lerp(lightSandColor, Color.SkyBlue, (float)Math.Pow(colorShiftInterpolant, 1.64f)) * opacity * 0.85f;
        }

        public static void DrawSpecialEffects(Vector2 center)
        {
            center.X -= 18f;
            center.Y += 18f;

            Vector2 start = center;
            Vector2 end = start - Vector2.UnitY * 580f;

            InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(0.84f);
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThinGlow);
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakFaded.Value;

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(start, end, i / 8f));
            
            SandPillarDrawer.Draw(points, -Main.screenPosition, 186);
            
            // Draw the portal.
            int sideCount = 512;
            float radius = Utils.GetLerpValue(0.5f, 0.95f, AnimationCompletion, true) * 90f;
            Utilities.GetCircleVertices(sideCount, radius, center - Vector2.UnitY * (radius + 60f), out var triangleIndices, out var vertices);

            CalamityUtils.CalculatePerspectiveMatricies(out Matrix view, out Matrix projection);
            InfernumEffectsRegistry.RealityTearVertexShader.SetShaderTexture(InfernumTextureRegistry.Water);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["uWorldViewProjection"].SetValue(view * projection);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["useOutline"].SetValue(false);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["uCoordinateZoom"].SetValue(3.2f);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["uTimeFactor"].SetValue(3.2f);
            InfernumEffectsRegistry.RealityTearVertexShader.UseSaturation(0.3f);
            InfernumEffectsRegistry.RealityTearVertexShader.Apply();

            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count, triangleIndices.ToArray(), 0, sideCount * 2);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<SandstormsCore>();
            Main.LocalPlayer.noThrow = 2;
            Main.LocalPlayer.cursorItemIconEnabled = true;
        }

        public override void MouseOverFar(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<SandstormsCore>();
            Main.LocalPlayer.noThrow = 2;
            Main.LocalPlayer.cursorItemIconEnabled = true;
        }
    }
}
