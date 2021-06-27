using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class PhantasmalSphereBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalSphere;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[5]++;
            if (projectile.alpha > 200)
                projectile.alpha = 200;
            projectile.alpha -= 5;
            if (projectile.alpha < 0)
                projectile.alpha = 0;

            projectile.scale = projectile.Opacity;
            projectile.tileCollide = projectile.scale >= 1f;
            if (projectile.ai[0] >= 0f)
            {
                projectile.ai[0] += 1f;
            }
            if (projectile.ai[0] == -1f)
            {
                projectile.frame = 1;
                projectile.extraUpdates = 1;
            }
            else if (projectile.ai[0] < 30f)
                projectile.position = Main.npc[(int)projectile.ai[1]].Center - new Vector2(projectile.width, projectile.height) / 2f - projectile.velocity;
            else
            {
                int num3 = projectile.frameCounter + 1;
                projectile.frameCounter = num3;
                if (num3 >= 6)
                {
                    projectile.frameCounter = 0;
                    num3 = projectile.frame + 1;
                    projectile.frame = num3;
                    if (num3 >= 2)
                    {
                        projectile.frame = 0;
                    }
                }
            }
            if (projectile.alpha < 40)
            {
                int num3;
                for (int num792 = 0; num792 < 2; num792 = num3 + 1)
                {
                    float num793 = (float)Main.rand.NextDouble() * 1f - 0.5f;
                    if (num793 < -0.5f)
                    {
                        num793 = -0.5f;
                    }
                    if (num793 > 0.5f)
                    {
                        num793 = 0.5f;
                    }
                    Vector2 value20 = new Vector2(-projectile.width * 0.65f * projectile.scale, 0f).RotatedBy(num793 * MathHelper.TwoPi, default).RotatedBy(projectile.velocity.ToRotation(), default);
                    int num794 = Dust.NewDust(projectile.Center - Vector2.One * 5f, 10, 10, 229, -projectile.velocity.X / 3f, -projectile.velocity.Y / 3f, 150, Color.Transparent, 0.7f);
                    Main.dust[num794].velocity = Vector2.Zero;
                    Main.dust[num794].position = projectile.Center + value20;
                    Main.dust[num794].noGravity = true;
                    num3 = num792;
                }
                return false;
            }
            return false;
        }
    }
}
