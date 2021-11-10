using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class ShadowBlast : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Bolt");
            Main.projFrames[projectile.type] = 3;
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

            projectile.velocity *= 1.018f;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

		public override void Kill(int timeLeft)
		{
            for (int i = 0; i < 5; i++)
			{
                Dust fire = Dust.NewDustDirect(projectile.Center - Vector2.One * 12f, 6, 6, 267);
                fire.color = Color.DarkGray;
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
