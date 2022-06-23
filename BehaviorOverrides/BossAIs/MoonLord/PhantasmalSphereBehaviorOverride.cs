using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalSphereBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalSphere;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI | ProjectileOverrideContext.ProjectilePreDraw;

        public override bool PreAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[0]++;

            projectile.alpha = Utils.Clamp(projectile.alpha - 5, 0, 200);
            projectile.scale = projectile.Opacity;
            projectile.tileCollide = projectile.scale >= 1f;
            if (projectile.ai[0] >= 0f)
            {
                projectile.ai[0]++;
            }
            if (projectile.ai[0] == -1f)
            {
                projectile.velocity *= 0.9f;
                projectile.frame = 1;
                projectile.extraUpdates = 1;
            }
            else if (projectile.ai[0] >= 30f)
            {
                projectile.frameCounter++;
                if (projectile.frameCounter >= 6)
                    projectile.frame = (projectile.frame + 1) % 2;
            }
            if (projectile.alpha < 40)
            {
                for (int i = 0; i < 2; i++)
                {
                    float angularDirectionOffset = Main.rand.NextFloat(MathHelper.TwoPi) + projectile.velocity.ToRotation();
                    Vector2 dustSpawnOffset = new Vector2(-projectile.width * 0.65f * projectile.scale, 0f).RotatedBy(angularDirectionOffset);
                    Dust electricity = Dust.NewDustDirect(projectile.Center - Vector2.One * 5f, 10, 10, 229, -projectile.velocity.X / 3f, -projectile.velocity.Y / 3f, 150, Color.Transparent, 0.7f);
                    electricity.velocity = Vector2.Zero;
                    electricity.position = projectile.Center + dustSpawnOffset;
                    electricity.noGravity = true;
                }
            }
            return false;
        }

        public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
        {
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            CalamityUtils.DrawAfterimagesCentered(projectile, 2, lightColor * 0.6f);
            return false;
        }
    }
}