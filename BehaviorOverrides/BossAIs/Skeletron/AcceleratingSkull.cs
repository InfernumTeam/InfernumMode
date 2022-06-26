using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Skeletron
{
    public class AcceleratingSkull : ModProjectile
    {
        public ref float IdealHorizontalOffsetSpeed => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Skull");
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
                Projectile.localAI[0] = 1f;
            }

            // Move to the sides.
            if (IdealHorizontalOffsetSpeed != -9999f)
            {
                Projectile.velocity.X = Utils.GetLerpValue(300f, 250f, Projectile.timeLeft, true) *
                    Utils.GetLerpValue(220f, 250f, Projectile.timeLeft, true) *
                    IdealHorizontalOffsetSpeed;
            }

            // And accelerate downward.
            if (Projectile.timeLeft > 270f)
                Projectile.velocity.Y *= 0.98f;
            else
            {
                if (Projectile.velocity.Y < 2.5f)
                    Projectile.velocity.Y = 2.5f;
                Projectile.velocity.Y *= 1.0285f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            for (int i = 0; i < 2; i++)
            {
                Dust magic = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 264);
                magic.velocity = -Projectile.velocity.RotatedByRandom(0.53f) * 0.15f;
                magic.scale = Main.rand.NextFloat(0.45f, 0.7f);
                magic.fadeIn = 0.6f;
                magic.noLight = true;
                magic.noGravity = true;
            }

            if (Projectile.timeLeft > 250f)
                Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.3f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.Purple;
            drawColor.A = 0;

            Utilities.DrawAfterimagesCentered(Projectile, drawColor, ProjectileID.Sets.TrailingMode[Projectile.type], 2);
            return true;
        }
    }
}
