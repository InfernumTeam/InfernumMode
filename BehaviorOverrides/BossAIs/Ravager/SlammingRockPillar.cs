using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class SlammingRockPillar : ModProjectile
    {
        public bool Vertical => projectile.ai[1] == 1f;

        public ref float Time => ref projectile.ai[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Rock Pillar");

        public override void SetDefaults()
        {
            projectile.width = 300;
            projectile.height = 60;
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
                float hoverInterpolant = (1f - Time / 45f) * 0.1f;
                Vector2 nextPosition = projectile.Center;

                if (Vertical)
                    nextPosition.X = MathHelper.Lerp(nextPosition.X, closestPlayer.Center.X, hoverInterpolant);
                else
                    nextPosition.Y = MathHelper.Lerp(nextPosition.Y, closestPlayer.Center.Y, hoverInterpolant);
                projectile.Center = nextPosition;
                projectile.velocity = Vector2.Zero;
            }

            if (Time == 54f)
            {
                if (Vertical)
                    projectile.velocity = Vector2.UnitY * (closestPlayer.Center.Y > projectile.Center.Y).ToDirectionInt() * 8f;
                else
                    projectile.velocity = Vector2.UnitX * (closestPlayer.Center.X > projectile.Center.X).ToDirectionInt() * 8f;
            }

            if (Vertical)
            {
                projectile.velocity.Y *= 1.018f;
                projectile.rotation = MathHelper.PiOver2;
                projectile.width = 300;
                projectile.height = 60;
            }
            else
            {
                projectile.velocity.X *= 1.018f;
                projectile.rotation = 0f;
                projectile.width = 60;
                projectile.height = 300;
            }

            // Smash into pieces and release a burst of fire if colliding with another pillar.
            if (Time >= 60f)
            {
                int otherPillar = -1;
                bool collidingWithOtherPillar = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (!Main.projectile[i].active || Main.projectile[i].type != projectile.type || i == projectile.whoAmI)
                        continue;

                    bool colliding = Main.projectile[i].Colliding(Main.projectile[i].Hitbox, projectile.Hitbox) ||
                        projectile.Colliding(projectile.Hitbox, Main.projectile[i].Hitbox);
                    if (!colliding || Main.projectile[i].ai[0] < 60f)
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

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tex = ModContent.GetTexture(Texture);
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(tex, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, tex.Size() * 0.5f, projectile.scale, 0, 0f);
            return false;
        }

        public override bool CanDamage() => Time >= 54f;

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
    }
}
