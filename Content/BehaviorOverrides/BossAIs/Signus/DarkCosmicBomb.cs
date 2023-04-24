using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class DarkCosmicBomb : ModProjectile
    {
        public ref float ExplosionRadius => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cosmic Bomb");
            Main.projFrames[Projectile.type] = 6;
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
            Projectile.timeLeft = 300;
            Projectile.MaxUpdates = 2;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 1f);

            Projectile.velocity *= 0.98f;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float explosionInterpolant = Utils.GetLerpValue(200f, 35f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 45f, Projectile.frameCounter, true);
            float circleFadeinInterpolant = Utils.GetLerpValue(0f, 0.15f, explosionInterpolant, true);
            float pulseInterpolant = Utils.GetLerpValue(0.75f, 0.85f, explosionInterpolant, true);
            float colorPulse = ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 6.3f + Projectile.identity) * 0.5f + 0.5f) * pulseInterpolant * 0.7f;
            lightColor = Color.Lerp(lightColor, Color.White, 0.4f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);

            if (explosionInterpolant > 0f)
            {
                Texture2D explosionTelegraphTexture = InfernumTextureRegistry.HollowCircleSoftEdge.Value;
                Vector2 scale = Vector2.One * ExplosionRadius / explosionTelegraphTexture.Size();
                Color explosionTelegraphColor = Color.Lerp(Color.Purple, Color.Black, colorPulse) * circleFadeinInterpolant;

                Main.spriteBatch.SetBlendState(BlendState.Additive);

                for (float dx = -6f; dx <= 6; dx += 3f)
                {
                    Main.spriteBatch.Draw(explosionTelegraphTexture, Projectile.Center - Main.screenPosition + Vector2.UnitX * dx, null, explosionTelegraphColor * 0.36f, 0f, explosionTelegraphTexture.Size() * 0.5f, scale, 0, 0f);
                    Main.spriteBatch.Draw(explosionTelegraphTexture, Projectile.Center - Main.screenPosition + Vector2.UnitY * dx, null, explosionTelegraphColor * 0.36f, 0f, explosionTelegraphTexture.Size() * 0.5f, scale, 0, 0f);
                }
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);

            // Create an explosion and two cosmic kunai.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                {
                    explosion.ModProjectile<CosmicExplosion>().MaxRadius = ExplosionRadius * 0.7f;
                });

                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CosmicExplosion>(), SignusBehaviorOverride.CosmicExplosionDamage, 0f);

                for (int i = 0; i < 2; i++)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(12f, 12f);
                    Utilities.NewProjectileBetter(Projectile.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<CosmicKunai>(), SignusBehaviorOverride.KunaiDamage, 0f);
                }
            }

            // Do some some mild screen-shake effects to accomodate the explosion.
            // This effect is set instead of added to to ensure separate explosions do not together create an excessive amount of shaking.
            float screenShakeFactor = Utils.Remap(Projectile.Distance(Main.LocalPlayer.Center), 2000f, 1300f, 0f, 9.6f);
            if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakeFactor)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakeFactor;
        }

        public override bool? CanDamage() => false;
    }
}
