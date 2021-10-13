using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalEyeBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalEye;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            projectile.alpha -= 40;
            if (projectile.alpha < 0)
                projectile.alpha = 0;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.tileCollide = projectile.localAI[0] >= 70f;
            projectile.localAI[0] += 1f;
            if (projectile.localAI[0] < 50f)
            {
                projectile.velocity.X = projectile.velocity.RotatedBy(projectile.ai[1]).X;
                projectile.velocity.X = (projectile.velocity * 40f + projectile.SafeDirectionTo(Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center) * 6).X / 41f;
                projectile.velocity.Y -= 0.07f;
            }
            else if (projectile.localAI[0] >= 50f)
            {
                projectile.velocity.X *= 0.95f;
                projectile.velocity.Y += 0.14f;
            }
            return false;
        }
    }
}
