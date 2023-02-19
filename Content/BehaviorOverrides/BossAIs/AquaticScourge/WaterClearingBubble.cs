using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Systems;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class WaterClearingBubble : ModProjectile, IPixelPrimitiveDrawer, IAdditiveDrawer
    {
        public PrimitiveTrailCopy WaterDrawer;

        public ref float Time => ref Projectile.ai[0];

        public static float Radius => 120f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Acid Bubble");

        public override void Load()
        {
            On.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += PrepareWater;
        }

        public void PrepareWater(On.Terraria.GameContent.Liquid.LiquidRenderer.orig_InternalDraw orig, Terraria.GameContent.Liquid.LiquidRenderer self, SpriteBatch spriteBatch, Vector2 drawOffset, int waterStyle, float globalAlpha, bool isBackgroundDraw)
        {
            // Make the nearby water clear.
            SulphuricWaterSafeZoneSystem.NearbySafeTiles.Clear();
            foreach (Projectile bubble in Utilities.AllProjectilesByID(Type))
            {
                if (bubble.Opacity <= 0f || !bubble.WithinRange(Main.LocalPlayer.Center, 2000f))
                    continue;

                Point p = bubble.Center.ToTileCoordinates();
                float power = 0f;
                if (SulphuricWaterSafeZoneSystem.NearbySafeTiles.TryGetValue(p, out float s))
                    power = s;

                SulphuricWaterSafeZoneSystem.NearbySafeTiles[p] = MathHelper.Max(power, bubble.scale * 0.8f);
            }

            orig(self, spriteBatch, drawOffset, waterStyle, globalAlpha, isBackgroundDraw);
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)Radius;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / 240f) * 4f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.scale = Projectile.Opacity;

            // Randomly emit bubbles.
            Vector2 bubbleSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(250f, 250f) * Projectile.scale;
            bubbleSpawnPosition += Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * 14f;
            if (!Main.rand.NextBool(3))
            {
                for (int i = 0; i < 7; i++)
                {
                    Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), bubbleSpawnPosition, Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f) * 0.75f, 411);
                    bubble.timeLeft = Main.rand.Next(8, 14);
                    bubble.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                }
            }

            Time++;
        }

        public float WidthFunction(float completionRatio) => Radius * Projectile.scale * CalamityUtils.Convert01To010(completionRatio);

        public Color ColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Pow(Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)), 3D) * 0.5f;
            return Color.Lerp(new Color(103, 218, 224), new Color(144, 114, 166), colorInterpolant) * Projectile.Opacity * 0.8f;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.BubblePop with { Pitch = -0.3f, Volume = 1.3f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            WaterDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVertexShader);

            InfernumEffectsRegistry.DukeTornadoVertexShader.UseImage1("Images/Misc/Perlin");
            List<Vector2> drawPoints = new();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTimeWrappedHourly * 2.2f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                Vector2 radius = Vector2.One * Radius;
                radius.Y *= MathHelper.Lerp(1f, 2f, (float)Math.Abs(Math.Cos(Main.GlobalTimeWrappedHourly * 1.9f)));

                for (int i = 0; i <= 8; i++)
                {
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * radius * 0.8f, Projectile.Center + offsetDirection * radius * 0.8f, i / 8f));
                }

                WaterDrawer.DrawPixelated(drawPoints, -Main.screenPosition, 20, adjustedAngle);
            }
        }

        // Draw an additive bubble overlay over the prims.
        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D bubble = InfernumTextureRegistry.Bubble.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color bubbleColor = Projectile.GetAlpha(Color.Lerp(Color.Cyan, Color.Wheat, 0.6f)) * 0.7f;
            Vector2 bubbleScale = Vector2.One * (Projectile.scale * 0.8f + (float)Math.Cos(Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity) * 0.04f);

            // Make the bubble scale squish a bit in one of the four cardinal directions for more a fluid aesthetic.
            Vector2 scalingDirection = -Vector2.UnitY.RotatedBy(Projectile.identity % 4 / 4f * MathHelper.TwoPi);
            bubbleScale += scalingDirection * (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 3.1f + Projectile.identity) * 0.5f + 0.5f) * 0.16f;

            Main.EntitySpriteDraw(bubble, drawPosition, null, bubbleColor, Projectile.rotation, bubble.Size() * 0.5f, bubbleScale, 0, 0);
        }
    }
}
