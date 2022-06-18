using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class ChargeTyphoon : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Typhoon");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 56;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 90;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            Time++;
            projectile.rotation += 0.4f * (projectile.velocity.X > 0).ToDirectionInt();
            projectile.velocity *= 0.987f;
            projectile.Opacity = Utils.InverseLerp(0f, 20f, Time, true);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int waveCount = 4;
                float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / waveCount;
                for (int i = 0; i < waveCount; i++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / waveCount + offsetAngle).ToRotationVector2() * 9f;
                    Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<SmallWave>(), 180, 0f);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return false;
        }
    }
}
