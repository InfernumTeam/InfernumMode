using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkMagicFlame : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Flame");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(0f, 24f, Time, true);

            // Attempt to hover above the target.
            Vector2 destination = Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center;
            if (Time < 15f)
            {
                float flySpeed = MathHelper.Lerp(8f, 20f, Time / 15f);
                projectile.velocity = (projectile.velocity * 29f + projectile.SafeDirectionTo(destination) * flySpeed) / 30f;
            }
            else if (projectile.velocity.Length() < 43f)
            {
                projectile.velocity *= 1.035f;
                if (Time < 45f)
                    projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(destination), 0.03f);
                projectile.tileCollide = true;
            }

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

		public override void Kill(int timeLeft)
		{
            for (int i = 0; i < 5; i++)
			{
                Dust fire = Dust.NewDustDirect(projectile.Center - Vector2.One * 12f, 6, 6, 267);
                fire.color = Color.Red;
                fire.noGravity = true;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1, Main.projectileTexture[projectile.type], false);
            return false;
        }
    }
}
