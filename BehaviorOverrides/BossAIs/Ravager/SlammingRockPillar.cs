using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class SlammingRockPillar : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Rock Pillar");

        public override void SetDefaults()
        {
            projectile.width = 60;
            projectile.height = 300;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.024f, 0f, 1f);
            Time++;

            // Hover into position before slamming.
            if (Time < 45f)
            {
                Vector2 nextPosition = projectile.Center;
                nextPosition.Y = MathHelper.Lerp(nextPosition.Y, closestPlayer.Center.Y, (1f - Time / 45f) * 0.1f);
                projectile.Center = nextPosition;
                projectile.velocity = Vector2.Zero;
            }

            if (Time == 54f)
                projectile.velocity = Vector2.UnitX * (closestPlayer.Center.X > projectile.Center.X).ToDirectionInt() * 8f;
            projectile.velocity.X *= 1.018f;

            // Smash into pieces and release a burst of fire if colliding with another pillar.
            if (Time >= 60f)
            {
                int otherPillar = -1;
                bool collidingWithOtherPillar = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (!Main.projectile[i].active || Main.projectile[i].type != projectile.type || i == projectile.whoAmI)
                        continue;

                    if (!Main.projectile[i].Hitbox.Intersects(projectile.Hitbox) || Main.projectile[i].ai[0] < 60f)
                        continue;

                    collidingWithOtherPillar = true;
                    otherPillar = i;
                    break;
                }

                if (collidingWithOtherPillar)
                {
                    // Create the cinder burst.
                    Main.PlaySound(SoundID.DD2_ExplosiveTrapExplode, projectile.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int cinderCount = 4;
                        float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / cinderCount;
                        for (int i = 0; i < cinderCount; i++)
                        {
                            Vector2 cinderShootVelocity = (MathHelper.TwoPi * i / cinderCount + offsetAngle).ToRotationVector2() * 9f;
                            Utilities.NewProjectileBetter(projectile.Center, cinderShootVelocity, ModContent.ProjectileType<DarkMagicCinder>(), 180, 0f);
                        }
                    }

                    // Create rock particles.
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 rockSpawnPosition = projectile.Center + Main.rand.NextVector2Circular(projectile.width, projectile.height) * 0.5f;
                        Vector2 rockVelocity = -Vector2.UnitY.RotatedByRandom(0.57f) * Main.rand.NextFloat(7f, 11f);
                        rockVelocity.X += Math.Abs(projectile.velocity.X) * Main.rand.NextFloat(0.67f, 0.95f) * Main.rand.NextBool().ToDirectionInt();
                        GeneralParticleHandler.SpawnParticle(new StoneDebrisParticle2(rockSpawnPosition, rockVelocity, Color.SandyBrown, 1.1f, 180));
                    }

                    Main.projectile[otherPillar].Kill();
                    projectile.Kill();
                }
            }
        }

        public override bool CanDamage() => Time >= 54f;

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
    }
}
