using System.IO;
using CalamityMod;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DemonicBomb : ModProjectile
    {
        public bool ExplodeIntoDarts;

        public ref float ExplosionRadius => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Demonic Bomb");
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
            writer.Write(ExplodeIntoDarts);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
            ExplodeIntoDarts = reader.ReadBoolean();
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.08f, 0f, 0.55f);

            Projectile.velocity *= 0.99f;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

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
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Texture2D explosionTelegraphTexture = InfernumTextureRegistry.HollowCircleSoftEdge.Value;
                Vector2 scale = Vector2.One * ExplosionRadius / explosionTelegraphTexture.Size() * 1.2f;
                Color explosionTelegraphColor = Color.Lerp(Color.Purple, Color.Red, colorPulse) * circleFadeinInterpolant;
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
                    explosion.ModProjectile<DemonicExplosion>().MaxRadius = ExplosionRadius * 0.7f;
                });
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), SupremeCalamitasBehaviorOverride.DemonicExplosionDamage, 0f);

                if (ExplodeIntoDarts)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 dartVelocity = (TwoPi * i / 6f).ToRotationVector2() * 7.4f;
                        Utilities.NewProjectileBetter(Projectile.Center, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), SupremeCalamitasBehaviorOverride.BrimstoneDartDamage, 0f);
                    }
                }
            }

            // Do some some mild screen-shake effects to accomodate the explosion.
            // This effect is set instead of added to to ensure separate explosions do not together create an excessive amount of shaking.
            float screenShakeFactor = Utils.Remap(Projectile.Distance(Main.LocalPlayer.Center), 2000f, 1300f, 0f, 11f);
            if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakeFactor)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakeFactor;
        }

        public override bool? CanDamage() => false;
    }
}
