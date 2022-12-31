using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DemonicTelegraphLine : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public ref float BombRadius => ref Projectile.localAI[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
        }

        public override void AI()
        {
            Projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (Projectile.localAI[1] != 0f)
                return;

            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 bombShootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 23f;
                int bomb = Utilities.NewProjectileBetter(Projectile.Center, bombShootVelocity, ModContent.ProjectileType<DemonicBomb>(), 0, 0f);
                if (Main.projectile.IndexInRange(bomb))
                {
                    Main.projectile[bomb].ai[0] = BombRadius;
                    Main.projectile[bomb].timeLeft = Main.rand.Next(135, 185);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphWidth = MathHelper.Lerp(0.3f, 3f, CalamityUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            Main.spriteBatch.DrawLineBetter(start, end, Color.Red, telegraphWidth);
            return false;
        }
    }
}