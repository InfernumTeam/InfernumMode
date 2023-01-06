using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.HallowedMimic
{
    public class FlyingKnife : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 270;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Flying Knife");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
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
            Projectile.rotation += Projectile.velocity.X * 0.035f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Time, true);

            // Harass the nearest player.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            float inertia = Projectile.WithinRange(target.Center, 250f) ? 15f : 45f;
            if (Time >= 60f)
                Projectile.velocity = (Projectile.velocity * (inertia - 1f) + Projectile.SafeDirectionTo(target.Center) * 14f) / inertia;
            if ((Time % 15f == 14f || Projectile.velocity.Length() < 9f) && !Projectile.WithinRange(target.Center, 270f))
            {
                Vector2 impulse = Projectile.SafeDirectionTo(target.Center) * 5f;
                Projectile.velocity = (Projectile.velocity + impulse).ClampMagnitude(9.1f, 21.5f);
                Projectile.netUpdate = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
