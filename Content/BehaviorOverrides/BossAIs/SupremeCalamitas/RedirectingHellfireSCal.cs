using CalamityMod.NPCs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedirectingHellfireSCal : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Hellfire");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            // Die of Supreme Cataclysm is not present.
            if (!NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()))
            {
                Projectile.Kill();
                return;
            }

            CreateVisuals();
            PerformMovement();
            Time++;
        }

        public void CreateVisuals()
        {
            if (Main.dedServ)
                return;

            // Emit idle dust.
            for (int i = 0; i < 2; i++)
            {
                Dust hellfire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), 267);
                hellfire.velocity = Main.rand.NextVector2Circular(2f, 2f) - Projectile.velocity.SafeNormalize(Vector2.Zero) * 1.6f;
                hellfire.scale = Main.rand.NextFloat(0.7f, 0.8f);
                hellfire.color = Color.Lerp(Color.OrangeRed, Color.Red, Main.rand.NextFloat());
                hellfire.noGravity = true;
            }

            // Emit crimson light.
            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);

            // Spin depending on direction.
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * -0.15f;
        }


        public void PerformMovement()
        {
            int initialAccelerationTime = 30;
            int redirectTime = 70;
            float maxRedirectSpeed = 15f;
            float maxAccelerateSpeed = 37f;
            float currentSpeed = Projectile.velocity.Length();

            // Accelerate a little bit prior to redirecting.
            if (Time < initialAccelerationTime && currentSpeed < maxRedirectSpeed)
                Projectile.velocity *= 1.012f;

            // Try to home in on the target after redirecting. Since this is supposed to be a redirect and not a proper homing attack
            // homing does not happen when close to the target.
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Time >= initialAccelerationTime && Time < initialAccelerationTime + redirectTime && !Projectile.WithinRange(target.Center, 380f))
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * currentSpeed, 0.09f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * currentSpeed;
            }

            // After redirecting, accelerate a bit.
            if (Time >= initialAccelerationTime + redirectTime && currentSpeed < maxAccelerateSpeed)
                Projectile.velocity *= 1.028f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }
    }
}
