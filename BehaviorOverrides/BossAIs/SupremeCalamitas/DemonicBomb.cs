using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DemonicBomb : ModProjectile
    {
        public float ExplosionRadius => projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demonic Bomb");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;

        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 900;
            projectile.Opacity = 0f;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.08f, 0f, 1f);

            projectile.velocity *= 0.99f;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(projectile.Center, Color.Yellow.ToVector3() * 0.5f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float explosionInterpolant = Utils.InverseLerp(200f, 35f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 45f, projectile.frameCounter, true);
            float circleFadeinInterpolant = Utils.InverseLerp(0f, 0.15f, explosionInterpolant, true);
            float pulseInterpolant = Utils.InverseLerp(0.75f, 0.85f, explosionInterpolant, true);
            float colorPulse = ((float)Math.Sin(Main.GlobalTime * 6.3f + projectile.identity) * 0.5f + 0.5f) * pulseInterpolant;
            lightColor = Color.Lerp(lightColor, Color.White, 0.4f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);

            if (explosionInterpolant > 0f)
            {
                Texture2D explosionTelegraphTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/HollowCircleSoftEdge");
                Vector2 scale = Vector2.One * ExplosionRadius / explosionTelegraphTexture.Size();
                Color explosionTelegraphColor = Color.Lerp(Color.Purple, Color.Red, colorPulse) * circleFadeinInterpolant;

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(explosionTelegraphTexture, projectile.Center - Main.screenPosition, null, explosionTelegraphColor, 0f, explosionTelegraphTexture.Size() * 0.5f, scale, 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int explosion = Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 900, 0f);
                if (Main.projectile.IndexInRange(explosion))
                    Main.projectile[explosion].ModProjectile<DemonicExplosion>().MaxRadius = ExplosionRadius * 0.7f;
            }

            // Do some some mild screen-shake effects to accomodate the explosion.
            // This effect is set instead of added to to ensure separate explosions do not together create an excessive amount of shaking.
            float screenShakeFactor = Utilities.Remap(projectile.Distance(Main.LocalPlayer.Center), 2000f, 1300f, 0f, 5f);
            if (Main.LocalPlayer.Calamity().GeneralScreenShakePower < screenShakeFactor)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = screenShakeFactor;
        }

        public override bool CanDamage() => false;
    }
}
