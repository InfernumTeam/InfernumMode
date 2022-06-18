using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DukeFishron
{
    public class TyphoonBlade : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Typhoon Blade");
            Main.projFrames[projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 56;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 270;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = (projectile.frameCounter / 4) % Main.projFrames[projectile.type];

            Time++;
            projectile.rotation += 0.4f * (projectile.velocity.X > 0).ToDirectionInt();
            projectile.Opacity = Utils.InverseLerp(0f, 30f, Time, true) * Utils.InverseLerp(0f, 16f, projectile.timeLeft, true);

            if (projectile.timeLeft < 90f)
            {
                if (projectile.velocity.Length() < 15f)
                    projectile.velocity *= 1.016f;
            }
            else if (Time > 40f)
            {
                float oldSpeed = projectile.velocity.Length();
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                projectile.velocity = (projectile.velocity * 49f + projectile.SafeDirectionTo(target.Center) * oldSpeed) / 50f;
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return false;
        }
    }
}
