using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class VoidTentacle : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Void Tentacle");

        public override void SetDefaults()
        {
            projectile.height = 160;
            projectile.width = 160;
            projectile.melee = true;
            projectile.hostile = true;
            projectile.MaxUpdates = 3;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            Vector2 originalCenter = projectile.Center;
            projectile.scale = 1f - projectile.localAI[0];
            projectile.width = (int)(20f * projectile.scale);
            projectile.height = projectile.width;
            projectile.position.X = originalCenter.X - (float)(projectile.width / 2);
            projectile.position.Y = originalCenter.Y - (float)(projectile.height / 2);

            if (projectile.localAI[0] < 0.1)
            {
                projectile.localAI[0] += 0.01f;
            }
            else
            {
                projectile.localAI[0] += 0.025f;
            }
            if (projectile.localAI[0] >= 0.95f)
            {
                projectile.Kill();
            }
            projectile.velocity.X = projectile.velocity.X + projectile.ai[0] * 1.5f;
            projectile.velocity.Y = projectile.velocity.Y + projectile.ai[1] * 1.5f;
            if (projectile.velocity.Length() > 16f)
            {
                projectile.velocity.Normalize();
                projectile.velocity *= 16f;
            }

            projectile.ai[0] *= 1.05f;
            projectile.ai[1] *= 1.05f;
            if (projectile.scale < 1f)
            {
                int i = 0;
                while (i < projectile.scale * 4f)
                {
                    int dustID = Main.rand.NextBool(5) ? 199 : 175;
                    int idx = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, dustID, projectile.velocity.X, projectile.velocity.Y, 100, default, 1.1f);
                    Main.dust[idx].position = (Main.dust[idx].position + projectile.Center) / 2f;
                    Main.dust[idx].noGravity = true;
                    Main.dust[idx].velocity *= 0.1f;
                    Main.dust[idx].velocity -= projectile.velocity * (1.3f - projectile.scale);
                    Main.dust[idx].fadeIn = 100;
                    Main.dust[idx].scale += projectile.scale * 0.75f;
                    i++;
                }
            }
        }
    }
}
