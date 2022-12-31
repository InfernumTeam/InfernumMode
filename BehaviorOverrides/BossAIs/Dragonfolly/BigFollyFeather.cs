using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class BigFollyFeather : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Feather");
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.scale = 0.96f;
            Projectile.timeLeft = 105;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
                Projectile.localAI[0] = 1f;
            }

            Projectile.velocity.Y += MathHelper.ToRadians(2.4f);
            Vector2 movementDirection = new(-(float)Math.Sin(Projectile.velocity.Y * 2f) * 4f, Math.Abs((float)Math.Cos(Projectile.velocity.Y * 2f)) * 6f);
            Vector2 collisionDirection = Collision.TileCollision(Projectile.position, movementDirection, (int)(Projectile.width * Projectile.scale), (int)(Projectile.height * Projectile.scale));
            if (movementDirection != collisionDirection)
                Projectile.velocity.Y = -1f;

            Projectile.position += movementDirection;
            Projectile.rotation = movementDirection.ToRotation() - MathHelper.PiOver4;

            if (Projectile.timeLeft < 30)
                Projectile.alpha = Utils.Clamp(Projectile.alpha + 10, 0, 255);

            if (Main.rand.NextBool(8))
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
        }

        public override void Kill(int timeLeft)
        {
            // Explode and fire a red lightning spark at the nearest player.
            for (int i = 0; i < 7; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 267, Main.rand.NextVector2Circular(2f, 2f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.7f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.5f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }

            for (float speed = 2f; speed <= 6f; speed += 0.7f)
            {
                float lifePersistance = Main.rand.NextFloat(0.8f, 1.7f);
                for (int i = 0; i < 40; i++)
                {
                    Dust energy = Dust.NewDustPerfect(Projectile.Center, 267);
                    energy.velocity = (MathHelper.TwoPi * i / 40f).ToRotationVector2() * speed;
                    energy.noGravity = true;
                    energy.color = Main.hslToRgb(Main.rand.NextFloat(0f, 0.08f), 0.85f, 0.6f);
                    energy.fadeIn = lifePersistance;
                    energy.scale = 1.4f;
                }
            }

            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 lightningVelocity = Projectile.SafeDirectionTo(target.Center) * 1.35f;
            Utilities.NewProjectileBetter(Projectile.Center, lightningVelocity, ModContent.ProjectileType<RedLightning>(), 250, 0f, -1, lightningVelocity.ToRotation(), Main.rand.Next(100));
        }
    }
}
