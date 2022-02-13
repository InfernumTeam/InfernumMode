using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class SulphurousPoisonCloud : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Cloud");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 1800;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, 0.4f, 0.54f, 0.21f);

            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 9 % Main.projFrames[projectile.type];

            projectile.velocity *= 0.995f;

            if (projectile.timeLeft < 180)
            {
                projectile.damage = 0;
                projectile.Opacity = MathHelper.Clamp(projectile.Opacity - 0.03f, 0f, 1f);
                if (projectile.Opacity <= 0f)
                    projectile.Kill();
            }
            else if (projectile.alpha > 30)
            {
                projectile.alpha -= 30;
                if (projectile.alpha < 30)
                {
                    projectile.alpha = 30;
                }
            }
        }

        public override bool CanHitPlayer(Player target) => projectile.timeLeft >= 180;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Venom, 300);
    }
}
