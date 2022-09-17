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
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 66;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 840;
            Projectile.penetrate = -1;
        }

        Vector2 initialVelocity = new(0f, 0f);

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0f, Projectile.Opacity * 0.5f, Projectile.Opacity * 0.5f);
            if (Projectile.frameCounter == 0f)
                initialVelocity = Projectile.velocity;

            Projectile.frameCounter++;
            if (Projectile.ai[0] == 1f)
                Projectile.velocity = initialVelocity.RotatedBy(-(MathHelper.TwoPi - (Math.Log(Projectile.frameCounter) * MathHelper.TwoPi + 1)));
            else
                Projectile.velocity = initialVelocity.RotatedBy(MathHelper.TwoPi - (Math.Log(Projectile.frameCounter) * MathHelper.TwoPi + 1));

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            int frame = (int)(Projectile.frameCounter / 5f);
            if (frame > 3)
                frame -= (int)Math.Floor(frame / 4f) * 4;
            Projectile.frame = frame;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
