using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DemonicTelegraphLine : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public ref float Lifetime => ref projectile.ai[1];

        public ref float BombRadius => ref projectile.localAI[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 900;
        }

        public override void AI()
        {
            projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 3f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
            if (Time >= Lifetime)
                projectile.Kill();

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (projectile.localAI[1] != 0f)
                return;

            Main.PlaySound(SoundID.Item74, projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 bombShootVelocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * 23f;
                int bomb = Utilities.NewProjectileBetter(projectile.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f);
                if (Main.projectile.IndexInRange(bomb))
                {
                    Main.projectile[bomb].ai[0] = BombRadius;
                    Main.projectile[bomb].timeLeft = Main.rand.Next(135, 185);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float telegraphInterpolant = Utils.InverseLerp(0f, Lifetime * 0.35f, Time, true);
            float telegraphWidth = MathHelper.Lerp(0.3f, 3f, CalamityUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            if (telegraphInterpolant < 1f)
            {
                Vector2 start = projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
                Vector2 end = projectile.Center + projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
                Main.spriteBatch.DrawLineBetter(start, end, Color.Red, telegraphWidth);
            }
            return false;
        }
    }
}