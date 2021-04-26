using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.FuckYouModeAIs.DoG.DoGAIClass;
namespace InfernumMode.FuckYouModeAIs.Sentinels
{
	public class CeaselessMagicRedirect : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 14;
            projectile.height = 34;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 420;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            Player player = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            projectile.velocity.X = projectile.DirectionTo(player.Center).X * 11f;
            if (projectile.timeLeft >= 290f && projectile.timeLeft < 330f)
            {
                projectile.rotation = projectile.rotation.AngleLerp(projectile.AngleTo(player.Center) + MathHelper.PiOver2, 0.5f);
            }
            else
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (projectile.timeLeft == 290f)
            {
                for (int i = 0; i < Main.rand.Next(1, 3); i++)
                {
                    Utilities.NewProjectileBetter(projectile.Center, new Vector2(0f, Main.rand.NextFloat(4f, 8f) * -1f).RotatedByRandom(MathHelper.ToRadians(20f)), ModContent.ProjectileType<CeaselessMagicFall>(), cvDarkMatterDamage, 0f);
                }
                float resSize = Main.screenWidth * Main.screenHeight;
                resSize /= resolutionConstant * 2f;
                projectile.velocity = (projectile.rotation - MathHelper.PiOver2).ToRotationVector2().RotatedByRandom(MathHelper.ToRadians(10f)) * velocityConstant * resSize;

                for (int i = 0; i < 12; i++)
                {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, 173, Vector2.One.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(5f, 8f));
                    dust.noGravity = true;
                }
            }
            if (projectile.timeLeft < 51)
                projectile.alpha += 5;
            Lighting.AddLight(projectile.Center, 0.65f, 0.12f, 0.6f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }
    }
}
