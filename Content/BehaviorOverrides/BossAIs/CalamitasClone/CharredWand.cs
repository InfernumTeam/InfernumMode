using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CharredWand : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Charred Wand");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.scale = 0.8f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 84;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            Projectile.rotation += Projectile.velocity.X * 0.03f;
            Projectile.velocity *= 0.986f;

            // Jitter before exploding.
            Projectile.Center += Main.rand.NextVector2Circular(1f, 1f) * (1f - Projectile.Opacity) * 2.5f;

            Time++;
        }
        
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.Red with { A = 0 }, lightColor, (1f - Projectile.Opacity) * 10f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            // Do funny screen stuff.
            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
            ScreenEffectSystem.SetFlashEffect(Projectile.Center, 2f, 45);

            SoundEngine.PlaySound(SCalBrimstoneGigablast.ImpactSound, Projectile.Center);

            Utilities.CreateShockwave(Projectile.Center, 2, 8, 120f, false);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 35; i++)
                {
                    Vector2 cinderVelocity = (MathHelper.TwoPi * i / 35f).ToRotationVector2() * 13.5f;
                    Utilities.NewProjectileBetter(Projectile.Center, cinderVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 155, 0f);
                }
            }

            for (int i = 0; i < 20; i++)
            {
                Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Red;
                CloudParticle fireCloud = new(Projectile.Center, (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 9f, fireColor, Color.DarkGray, 45, Main.rand.NextFloat(1.9f, 2.3f));
                GeneralParticleHandler.SpawnParticle(fireCloud);
            }
        }
    }
}
