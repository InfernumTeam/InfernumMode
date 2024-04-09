using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class FollyFeather : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Feather");
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.scale = 0.667f;
            Projectile.timeLeft = 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
                Projectile.localAI[0] = 1f;
            }

            Projectile.velocity.Y += ToRadians(1.5f);
            Vector2 movementDirection = new(-Sin(Projectile.velocity.Y * 2f) * 4f, Math.Abs(Cos(Projectile.velocity.Y * 2f)) * 6f);
            Vector2 collisionDirection = Collision.TileCollision(Projectile.position, movementDirection, (int)(Projectile.width * Projectile.scale), (int)(Projectile.height * Projectile.scale));
            if (movementDirection != collisionDirection)
                Projectile.velocity.Y = -1f;

            Projectile.position += movementDirection;
            Projectile.rotation = movementDirection.ToRotation();

            if (Projectile.timeLeft < 30)
                Projectile.alpha = Utils.Clamp(Projectile.alpha + 10, 0, 255);

            if (Main.rand.NextBool(24))
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
        }
    }
}
