using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.GiantClam
{
    public class PearlSwirl : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Pearl Swirl");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 40;
            projectile.height = 66;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 840;
            projectile.penetrate = -1;
        }

        Vector2 initialVelocity = new Vector2(0f, 0f);

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, 0f, projectile.Opacity * 0.5f, projectile.Opacity * 0.5f);
            if (projectile.frameCounter == 0f)
                initialVelocity = projectile.velocity;

            projectile.frameCounter++;
            if (projectile.ai[0] == 1f)
                projectile.velocity = initialVelocity.RotatedBy(-(MathHelper.TwoPi - (Math.Log(projectile.frameCounter) * MathHelper.TwoPi + 1)));
            else
                projectile.velocity = initialVelocity.RotatedBy(MathHelper.TwoPi - (Math.Log(projectile.frameCounter) * MathHelper.TwoPi + 1));

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            int frame = (int)(projectile.frameCounter / 5f);
            if (frame > 3)
                frame -= (int)Math.Floor(frame / 4f) * 4;
            projectile.frame = frame;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
    }
}
