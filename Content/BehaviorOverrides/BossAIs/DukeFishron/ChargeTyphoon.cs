using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DukeFishron
{
    public class ChargeTyphoon : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Typhoon");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 56;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
            Projectile.penetrate = -1;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation += 0.4f * (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.velocity *= 0.987f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int waveCount = 4;
                float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / waveCount;
                for (int i = 0; i < waveCount; i++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / waveCount + offsetAngle).ToRotationVector2() * 9f;
                    Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<SmallWave>(), 180, 0f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
