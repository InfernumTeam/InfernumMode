using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolyBomb : ModProjectile, ISpecializedDrawRegion
    {
        public float ExplosionRadius => Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Bomb");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;

        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 0.48f);

            Projectile.velocity *= 0.99f;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (ProvidenceBehaviorOverride.IsEnraged)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/HolyBombNight").Value;

            // Make the light color considerably brighter, especially at the beginning of the bomb's lifetime.
            lightColor = Color.Lerp(lightColor, Color.White, 0.55f);
            lightColor.A = (byte)(lightColor.A / Utils.Remap(Time, 0f, 45f, 6f, 2f));

            Utilities.DrawAfterimagesCentered(Projectile, lightColor * Projectile.Opacity, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int explosionDamage = !ProvidenceBehaviorOverride.IsEnraged ? 350 : 600;

                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                {
                    explosion.ModProjectile<HolySunExplosion>().MaxRadius = ExplosionRadius * 0.7f;
                });
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HolySunExplosion>(), explosionDamage, 0f);
            }

            // Do some some mild screen-shake effects to accomodate the explosion.
            // This effect is set instead of added to to ensure separate explosions do not together create an excessive amount of shaking.
            float screenShakeFactor = Utils.Remap(Projectile.Distance(Main.LocalPlayer.Center), 2000f, 1300f, 0f, 8f);
            if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakeFactor)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakeFactor;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => false;

        public void SpecialDraw(SpriteBatch spriteBatch)
        {
            float explosionInterpolant = Utils.GetLerpValue(200f, 35f, Projectile.timeLeft, true);
            float pulseInterpolant = Utils.GetLerpValue(0.75f, 0.85f, explosionInterpolant, true);
            float circleFadeinInterpolant = Utils.GetLerpValue(0f, 0.15f, explosionInterpolant, true);
            float colorPulse = ((float)Math.Cos(Main.GlobalTimeWrappedHourly * 7.2f + Projectile.identity) * 0.5f + 0.5f) * pulseInterpolant * 0.6f;
            colorPulse += (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 6.1f + Projectile.identity * 1.3f) * 0.5f + 0.5f) * 0.4f;

            if (explosionInterpolant > 0f)
            {
                Color explosionTelegraphColor = Color.Lerp(Color.Yellow, Color.Orange, colorPulse) * circleFadeinInterpolant;
                if (ProvidenceBehaviorOverride.IsEnraged)
                    explosionTelegraphColor = Color.Lerp(Color.Cyan, Color.Lime, colorPulse * 0.67f) * circleFadeinInterpolant;
                explosionTelegraphColor = Color.Lerp(explosionTelegraphColor, Color.White, (1f - pulseInterpolant) * 0.45f);

                Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
                Texture2D noise = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes2").Value;
                Effect fireballShader = InfernumEffectsRegistry.FireballShader.GetShader().Shader;

                Vector2 scale = Vector2.One * ExplosionRadius / invisible.Size() * circleFadeinInterpolant * Projectile.Opacity * 1.67f;
                fireballShader.Parameters["sampleTexture2"].SetValue(noise);
                fireballShader.Parameters["mainColor"].SetValue(explosionTelegraphColor.ToVector3());
                fireballShader.Parameters["resolution"].SetValue(Vector2.One * 250f);
                fireballShader.Parameters["speed"].SetValue(0.76f);
                fireballShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
                fireballShader.Parameters["zoom"].SetValue(0.0004f);
                fireballShader.Parameters["dist"].SetValue(60f);
                fireballShader.Parameters["opacity"].SetValue(circleFadeinInterpolant * Projectile.Opacity / 0.48f * 0.335f);
                fireballShader.CurrentTechnique.Passes[0].Apply();

                Vector2 drawPosition = Projectile.Center + Vector2.UnitY * Projectile.scale * 18f - Main.screenPosition;
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, Projectile.rotation, invisible.Size() * 0.5f, scale, 0, 0f);
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, Projectile.rotation, invisible.Size() * 0.5f, scale * 0.5f, 0, 0f);
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, Projectile.rotation, invisible.Size() * 0.5f, scale * 0.32f, 0, 0f);
            }
        }

        public void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.EnterShaderRegion(BlendState.Additive);
        }
    }
}
