using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class FollyFeather : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Feather");
            Main.projFrames[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 22;
            projectile.hostile = false;
            projectile.friendly = false;
            projectile.tileCollide = true;
            projectile.scale = 0.667f;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                projectile.frame = Main.rand.Next(Main.projFrames[projectile.type]);
                projectile.localAI[0] = 1f;
            }

            projectile.velocity.Y += MathHelper.ToRadians(1.5f);
            Vector2 movementDirection = new Vector2(-(float)Math.Sin(projectile.velocity.Y * 2f) * 4f, Math.Abs((float)Math.Cos(projectile.velocity.Y * 2f)) * 6f);
            Vector2 collisionDirection = Collision.TileCollision(projectile.position, movementDirection, (int)(projectile.width * projectile.scale), (int)(projectile.height * projectile.scale));
            if (movementDirection != collisionDirection)
                projectile.velocity.Y = -1f;

            projectile.position += movementDirection;
            projectile.rotation = movementDirection.ToRotation();

            if (projectile.timeLeft < 30)
                projectile.alpha = Utils.Clamp(projectile.alpha + 10, 0, 255);

            if (Main.rand.NextBool(24))
            {
                Dust redLightning = Dust.NewDustPerfect(projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
        }
    }
}
