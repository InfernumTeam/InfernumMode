using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.GiantClam
{
    public class PearlSwirl : ModProjectile
    {
        public Vector2 InitialVelocity;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Pearl Swirl");
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
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0f, Projectile.Opacity * 0.5f, Projectile.Opacity * 0.5f);
            if (Projectile.frameCounter == 0f)
                InitialVelocity = Projectile.velocity;

            Projectile.frameCounter++;
            if (Projectile.ai[0] == 1f)
                Projectile.velocity = InitialVelocity.RotatedBy(-(TwoPi - (Math.Log(Projectile.frameCounter) * TwoPi + 1)));
            else
                Projectile.velocity = InitialVelocity.RotatedBy(TwoPi - (Math.Log(Projectile.frameCounter) * TwoPi + 1));

            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            int frame = (int)(Projectile.frameCounter / 5f);
            if (frame > 3)
                frame -= (int)Math.Floor(frame / 4f) * 4;
            Projectile.frame = frame;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
