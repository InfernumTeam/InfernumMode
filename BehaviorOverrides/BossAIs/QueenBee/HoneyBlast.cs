using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class HoneyBlast : ModProjectile
    {
        public bool Poisonous => Projectile.ai[0] == 1f;
        public ref float TotalBounces => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Honey Blast");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.scale = 1f;
            Projectile.tileCollide = true;
            Projectile.friendly = false;
            Projectile.hostile = true;
        }

        public override void AI()
        {
            Projectile.tileCollide = Projectile.timeLeft < 390;
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Poisonous)
                target.AddBuff(BuffID.Poisoned, 240);
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust ichor = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), 170);
                ichor.velocity = Main.rand.NextVector2Circular(3f, 3f);
                ichor.scale = 0.7f;
                ichor.fadeIn = 0.7f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (TotalBounces == 0f)
            {
                if (Projectile.velocity.X != oldVelocity.X)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = -oldVelocity.Y;

                if (Projectile.velocity.Y < 3f && Projectile.velocity.Y > -3f)
                    Projectile.velocity = Vector2.Zero;
            }
            else
                Projectile.velocity = Vector2.Zero;

            TotalBounces++;
            if (TotalBounces <= 2f)
                Projectile.netUpdate = true;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Poisonous ? Color.Green : Color.White;
            drawColor.A = 0;

            Utilities.DrawAfterimagesCentered(Projectile, drawColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return true;
        }
    }
}
