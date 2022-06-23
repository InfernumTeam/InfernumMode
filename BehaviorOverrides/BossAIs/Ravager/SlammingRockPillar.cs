using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class SlammingRockPillar : ModProjectile
    {
        public bool Vertical => Projectile.ai[1] == 1f;

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Rock Pillar");

        public override void SetDefaults()
        {
            Projectile.width = 300;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.024f, 0f, 1f);
            Time++;

            // Hover into position before slamming.
            if (Time < 45f)
            {
                float hoverInterpolant = (1f - Time / 45f) * 0.1f;
                Vector2 nextPosition = Projectile.Center;

                if (Vertical)
                    nextPosition.X = MathHelper.Lerp(nextPosition.X, closestPlayer.Center.X, hoverInterpolant);
                else
                    nextPosition.Y = MathHelper.Lerp(nextPosition.Y, closestPlayer.Center.Y, hoverInterpolant);
                Projectile.Center = nextPosition;
                Projectile.velocity = Vector2.Zero;
            }

            if (Time == 54f)
            {
                if (Vertical)
                    Projectile.velocity = Vector2.UnitY * (closestPlayer.Center.Y > Projectile.Center.Y).ToDirectionInt() * 8f;
                else
                    Projectile.velocity = Vector2.UnitX * (closestPlayer.Center.X > Projectile.Center.X).ToDirectionInt() * 8f;
            }

            if (Vertical)
            {
                Projectile.velocity.Y *= 1.018f;
                Projectile.rotation = MathHelper.PiOver2;
                Projectile.width = 300;
                Projectile.height = 60;
            }
            else
            {
                Projectile.velocity.X *= 1.018f;
                Projectile.rotation = 0f;
                Projectile.width = 60;
                Projectile.height = 300;
            }

            // Smash into pieces and release a burst of fire if colliding with another pillar.
            if (Time >= 60f)
            {
                int otherPillar = -1;
                bool collidingWithOtherPillar = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (!Main.projectile[i].active || Main.projectile[i].type != Projectile.type || i == Projectile.whoAmI)
                        continue;

                    bool colliding = Main.projectile[i].Colliding(Main.projectile[i].Hitbox, Projectile.Hitbox) ||
                        Projectile.Colliding(Projectile.Hitbox, Main.projectile[i].Hitbox);
                    if (!colliding || Main.projectile[i].ai[0] < 60f)
                        continue;

                    collidingWithOtherPillar = true;
                    otherPillar = i;
                    break;
                }

                if (collidingWithOtherPillar)
                {
                    // Create the cinder burst.
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int cinderCount = 4;
                        int cinderDamage = (int)(Projectile.damage * 0.8f);
                        float offsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / cinderCount;
                        for (int i = 0; i < cinderCount; i++)
                        {
                            Vector2 cinderShootVelocity = (MathHelper.TwoPi * i / cinderCount + offsetAngle).ToRotationVector2() * 9f;
                            Projectile.NewProjectile(Projectile.Center, cinderShootVelocity, ModContent.ProjectileType<DarkMagicCinder>(), cinderDamage, 0f);
                        }
                    }

                    // Create rock particles.
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 rockSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f;
                        Vector2 rockVelocity = -Vector2.UnitY.RotatedByRandom(0.57f) * Main.rand.NextFloat(7f, 11f);
                        rockVelocity.X += Math.Abs(Projectile.velocity.X) * Main.rand.NextFloat(0.67f, 0.95f) * Main.rand.NextBool().ToDirectionInt();
                        GeneralParticleHandler.SpawnParticle(new StoneDebrisParticle2(rockSpawnPosition, rockVelocity, Color.SandyBrown, 1.1f, 180));
                    }

                    Main.projectile[otherPillar].Kill();
                    Projectile.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(tex, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 54f;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
