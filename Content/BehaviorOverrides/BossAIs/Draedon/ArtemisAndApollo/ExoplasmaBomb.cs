using System.IO;
using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ExoplasmaBomb : ModProjectile
    {
        public ref float ExplosionRadius => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Exoplasma Bomb");
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
            Projectile.timeLeft = 900;
            Projectile.Opacity = 0f;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }

        public override void AI()
        {
            // Create a burst of dust on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 200, default, 1.7f);
                    plasma.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.width);
                    plasma.noGravity = true;
                    plasma.velocity *= 3f;

                    plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 100, default, 0.8f);
                    plasma.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.width);
                    plasma.velocity *= 2f;

                    plasma.noGravity = true;
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Green * 0.5f;
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                    plasma.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                    plasma.noGravity = true;
                    plasma.velocity *= 0.5f;
                }

                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = Clamp(Projectile.Opacity + 0.04f, 0f, 0.8f);

            Projectile.velocity *= 0.99f;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Make the bomb radius fade away if the projectile itself is fading away.
            if (Projectile.Infernum().FadeAwayTimer >= 1)
                ExplosionRadius *= 0.9f;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.4f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);

            float explosionInterpolant = Utils.GetLerpValue(200f, 35f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 45f, Projectile.frameCounter, true);
            float circleFadeinInterpolant = Utils.GetLerpValue(0f, 0.15f, explosionInterpolant, true);
            float pulseInterpolant = Utils.GetLerpValue(0.75f, 0.85f, explosionInterpolant, true);
            float colorPulse = (Sin(Main.GlobalTimeWrappedHourly * 6.3f + Projectile.identity) * 0.5f + 0.5f) * pulseInterpolant;
            if (explosionInterpolant > 0f)
            {
                Texture2D explosionTelegraphTexture = InfernumTextureRegistry.HollowCircleSoftEdge.Value;
                Vector2 scale = Vector2.One * ExplosionRadius / explosionTelegraphTexture.Size() * Projectile.Opacity * 1.2f;
                Color explosionTelegraphColor = Color.Lerp(Color.Lime, Color.Yellow, colorPulse) * circleFadeinInterpolant;
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(explosionTelegraphTexture, Projectile.Center - Main.screenPosition, null, explosionTelegraphColor, 0f, explosionTelegraphTexture.Size() * 0.5f, scale, 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                {
                    explosion.ModProjectile<ExoplasmaExplosion>().MaxRadius = ExplosionRadius * 0.7f;
                });
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ExoplasmaExplosion>(), DraedonBehaviorOverride.StrongerNormalShotDamage, 0f);
            }
        }

        public override bool? CanDamage() => false;
    }
}
