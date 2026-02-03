using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CrimsonMimic
{
    public class LifeDrainingField : ModProjectile
    {
        public float Radius => Utils.Remap(Time, 5f, 64f, 1f, 250f) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 240;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Life-Draining Field");

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            
        }

        public override void AI()
        {
            Time++;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            float flySpeed = Utils.Remap(Time, 0f, 40f, 0f, 4.5f);
            if (Time >= 6f)
                Projectile.velocity = Vector2.Zero.MoveTowards(target.Center - Projectile.Center, flySpeed);

            float dustCreationChance = Utils.GetLerpValue(0f, 20f, Time, true);
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextFloat() > dustCreationChance)
                    continue;

                Dust blood = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.RainbowMk2);
                blood.color = Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat(0.1f, 0.45f));
                blood.velocity = Main.rand.NextVector2Circular(0.7f, 0.7f);
                blood.scale = 1.1f;
                blood.noGravity = true;
            }

            for (int i = 0; i < 60; i++)
            {
                Vector2 dustSpawnOffset = (TwoPi * i / 60f).ToRotationVector2() * Radius;
                Dust blood = Dust.NewDustPerfect(Projectile.Center + dustSpawnOffset, DustID.RainbowMk2);
                blood.color = Color.Lerp(Color.Red, Color.IndianRed, Main.rand.NextFloat(0.2f, 0.65f));
                blood.velocity = Projectile.velocity + Vector2.UnitY * Main.rand.NextFloatDirection() * 2.5f;
                blood.scale = 0.6f;
                blood.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Time >= 45f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return LumUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);
        }
    }
}
