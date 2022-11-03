using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BaseEntities
{
    public abstract class BaseCinderProjectile : ModProjectile
    {
        public virtual int MinLifetime => 120;

        public virtual int MaxLifetime => 195;

        public virtual float MinRandomScale => 0.8f;

        public virtual float MaxRandomScale => 1.2f;

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
            Projectile.width = Projectile.height = 4;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = MaxLifetime + 1;
        }

        public override void AI()
        {
            // Don't draw cinders that are offscreen, for performance reasons.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 300;

            // Decide a variant to use on the first frame this projectile exists.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
                Projectile.localAI[0] = 1f;
            }

            // Make a decision for the lifetime for the cinder if one has not yet been made.
            if (Lifetime == 0f)
            {
                Lifetime = Main.rand.Next(MinLifetime, MaxLifetime);
                Projectile.netUpdate = true;
            }

            // Calculate scale of the cinder.
            else
            {
                Projectile.scale = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - 20f, Time, true);
                Projectile.scale *= MathHelper.Lerp(MinRandomScale, MaxRandomScale, Projectile.identity % 6f / 6f);
            }

            // Fly up and down.
            if (Math.Abs(Projectile.velocity.X) > 4f && Projectile.identity % 2 == 1)
                Projectile.velocity.Y += (float)Math.Sin(MathHelper.TwoPi * Time / 42f) * 0.0667f;

            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }
    }
}
