using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Leviathan
{
    public class AtlantisSpear2 : ModProjectile
    {
        public ref float Time => ref projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/Magic/AtlantisSpear";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Atlantis Spear");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 52;
            projectile.aiStyle = 4;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            aiType = ProjectileID.CrystalVileShardShaft;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (projectile.ai[0] == 0f)
            {
                projectile.alpha -= 50;
                if (projectile.alpha <= 0)
                {
                    projectile.alpha = 0;
                    projectile.ai[0] = 1f;
                    if (Time == 0f)
                    {
                        Time++;
                        projectile.position += projectile.velocity;
                    }
                }
            }
            else
            {
                if (projectile.alpha < 170 && projectile.alpha + 5 >= 170)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Dust water = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 206, projectile.velocity.X * 0.005f, projectile.velocity.Y * 0.005f, 200, default, 1f);
                        water.noGravity = true;
                        water.velocity *= 0.5f;
                    }
                }
                projectile.alpha += 7;
                if (projectile.alpha >= 255)
                {
                    projectile.Kill();
                }
            }
            if (Main.rand.NextBool(4))
            {
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, 206, projectile.velocity.X * 0.005f, projectile.velocity.Y * 0.005f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, projectile.alpha);

        public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, 206, projectile.oldVelocity.X * 0.005f, projectile.oldVelocity.Y * 0.005f);
            }
        }
    }
}
