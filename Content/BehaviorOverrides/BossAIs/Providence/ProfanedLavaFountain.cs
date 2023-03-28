using CalamityMod.DataStructures;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProfanedLavaFountain : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy LavaDrawer
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float GeyserHeight => ref Projectile.ai[1];

        public const float MaxGeyserHeight = 660f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Lava Fountain");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 120;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = (float)Math.Sin(Projectile.timeLeft / 120f * MathHelper.Pi) * 4f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (GeyserHeight < 2f)
                GeyserHeight = 2f;

            if (Time > 55f)
                GeyserHeight = MathHelper.Lerp(GeyserHeight, GeyserHeight * 1.44f, 0.1243f) + 2f;
            else
                Projectile.timeLeft++;

            // Create a splash of lava blobs.
            if (Time == 55f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSmallSound, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        // Release a bunch of lava particles from below.
                        int lavaLifetime = Main.rand.Next(120, 167);
                        float blobSize = MathHelper.Lerp(12f, 34f, (float)Math.Pow(Main.rand.NextFloat(), 1.85));
                        if (Main.rand.NextBool(6))
                            blobSize *= 1.4f;
                        Vector2 lavaVelocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 5f);
                        Utilities.NewProjectileBetter(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), lavaVelocity, ModContent.ProjectileType<ProfanedLavaBlob>(), ProvidenceBehaviorOverride.SmallLavaBlobDamage, 0f, -1, lavaLifetime, blobSize);
                    }
                }
            }

            if (Time == 0f)
            {
                Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item73, closestPlayer.Center);
            }

            Time++;
        }

        public Color ColorFunction(float completionRatio)
        {
            return Color.Wheat * Projectile.Opacity * 1.5f;
        }

        public float WidthFunction(float completionRatio) => MathHelper.Lerp(26f, 33f, (float)Math.Abs(Math.Cos(Main.GlobalTimeWrappedHourly * 2f))) * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = WidthFunction(0.6f);
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center - Vector2.UnitY * GeyserHeight;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time < 55f)
            {
                float telegraphFade = Utils.GetLerpValue(0f, 8f, Time, true) * Utils.GetLerpValue(55f, 45f, Time, true);
                float telegraphWidth = telegraphFade * 7.5f;
                Color telegraphColor = Color.Orange * telegraphFade;
                Vector2 start = Projectile.Center - Vector2.UnitY * 2000f;
                Vector2 end = Projectile.Center + Vector2.UnitY * 2000f;
                Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
            }
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            LavaDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.WoFGeyserVertexShader);

            InfernumEffectsRegistry.WoFGeyserVertexShader.UseSaturation(-1f);
            InfernumEffectsRegistry.WoFGeyserVertexShader.UseColor(Color.Lerp(Color.Orange, Color.Yellow, 0.6f));
            InfernumEffectsRegistry.WoFGeyserVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center - Vector2.UnitY * GeyserHeight, i / 8f));
            LavaDrawer.DrawPixelated(new BezierCurve(points.ToArray()).GetPoints(20), Vector2.UnitX * 10f - Main.screenPosition, 35);

            InfernumEffectsRegistry.WoFGeyserVertexShader.UseSaturation(1f);
            LavaDrawer.DrawPixelated(new BezierCurve(points.ToArray()).GetPoints(20), Vector2.UnitX * -10f - Main.screenPosition, 35);
        }
    }
}
