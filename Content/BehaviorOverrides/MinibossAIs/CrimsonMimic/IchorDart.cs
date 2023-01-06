using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CrimsonMimic
{
    public class IchorDart : ModProjectile
    {
        public bool HasSplit => Projectile.ai[1] == 1f;

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Dart");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Time++;

            Projectile.tileCollide = Time >= 75f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true);
            if (HasSplit)
                Projectile.Opacity = 1f;

            // Split into multiple darts.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= 40f && !HasSplit)
            {
                for (int i = 0; i < 5; i++)
                {
                    float shootOffsetAngle = MathHelper.Lerp(-0.54f, 0.54f, i / 4f);
                    Vector2 shootVelocity = Projectile.velocity.RotatedBy(shootOffsetAngle);
                    int splitDart = Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, Type, 115, 0f);
                    if (Main.projectile.IndexInRange(splitDart))
                        Main.projectile[splitDart].ai[1] = 1f;
                }
                Projectile.Kill();
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextFloat() > Projectile.Opacity)
                    continue;

                Dust ichor = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, 170);
                ichor.velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
                ichor.noGravity = true;
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
