using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class HoneyBlast : ModProjectile
    {
        public bool Poisonous => projectile.ai[0] == 1f;
        public ref float TotalBounces => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Honey Blast");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.ignoreWater = true;
            projectile.timeLeft = 420;
            projectile.scale = 1f;
            projectile.tileCollide = true;
            projectile.friendly = false;
            projectile.hostile = true;
        }

        public override void AI()
        {
            projectile.tileCollide = projectile.timeLeft < 390;
            if (projectile.velocity != Vector2.Zero)
                projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
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
                Dust ichor = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(4f, 4f), 170);
                ichor.velocity = Main.rand.NextVector2Circular(3f, 3f);
                ichor.scale = 0.7f;
                ichor.fadeIn = 0.7f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (TotalBounces == 0f)
            {
                if (projectile.velocity.X != oldVelocity.X)
                    projectile.velocity.X = -oldVelocity.X;
                if (projectile.velocity.Y != oldVelocity.Y)
                    projectile.velocity.Y = -oldVelocity.Y;

                if (projectile.velocity.Y < 3f && projectile.velocity.Y > -3f)
                    projectile.velocity = Vector2.Zero;
            }
            else
                projectile.velocity = Vector2.Zero;

            TotalBounces++;
            if (TotalBounces <= 2f)
                projectile.netUpdate = true;
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Color drawColor = Poisonous ? Color.Green : Color.White;
            drawColor.A = 0;

            Utilities.DrawAfterimagesCentered(projectile, drawColor, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return true;
        }
    }
}
