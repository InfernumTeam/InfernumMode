using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class CleansingFireball : ModProjectile
    {
        public bool InLava
        {
            get
            {
                IEnumerable<Projectile> lavaProjectiles = Utilities.AllProjectilesByID(ModContent.ProjectileType<ProfanedLava>());
                if (!lavaProjectiles.Any())
                    return false;

                return lavaProjectiles.Any(l => l.Colliding(l.Hitbox, Projectile.Hitbox));
            }
        }

        public bool CollidingWithWall => HasCollidedWithWall || Collision.SolidCollision(Projectile.TopLeft, Projectile.width, Projectile.height);

        public ref float Time => ref Projectile.ai[0];

        public bool HasCollidedWithWall
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cleansing Fireball");
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 240;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 300;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide the fireball's rotation.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 6 % Main.projFrames[Type];

            // Dissipate into ashes if inside of a wall.
            if (CollidingWithWall && Time >= 90f)
            {
                if (!HasCollidedWithWall)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath55, Projectile.Center);
                    HasCollidedWithWall = true;
                }

                // Release ashes.
                int ashCount = (int)Lerp(8f, 2f, Projectile.Opacity);
                for (int i = 0; i < ashCount; i++)
                {
                    Color startingColor = Color.Lerp(Color.Orange, Color.Gray, Main.rand.NextFloat(0.5f, 0.8f));
                    MediumMistParticle ash = new(Projectile.Center + Main.rand.NextVector2Circular(150f, 150f), Main.rand.NextVector2Circular(3f, 3f), startingColor, Color.DarkGray, Projectile.Opacity, 255f, Main.rand.NextFloatDirection() * 0.014f);
                    GeneralParticleHandler.SpawnParticle(ash);
                }

                Projectile.Opacity = Clamp(Projectile.Opacity - 0.085f, 0f, 1f);
                if (Projectile.Opacity <= 0f)
                    Projectile.Kill();
            }

            // Prepare to explode if inside the lava.
            else if (InLava)
            {
                Projectile.Opacity = Clamp(Projectile.Opacity - 0.085f, 0f, 1f);
                if (Projectile.Opacity <= 0f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with { Volume = 0.6f }, Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 15; i++)
                        {
                            // Release a bunch of lava particles from below.
                            int lavaLifetime = Main.rand.Next(120, 167);
                            float blobSize = Lerp(12f, 34f, Pow(Main.rand.NextFloat(), 1.85f));
                            if (Main.rand.NextBool(6))
                                blobSize *= 1.4f;
                            Vector2 lavaVelocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 5f);
                            Utilities.NewProjectileBetter(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), lavaVelocity, ModContent.ProjectileType<ProfanedLavaBlob>(), ProvidenceBehaviorOverride.SmallLavaBlobDamage, 0f, -1, lavaLifetime, blobSize);
                        }

                        // Release four cinders up from below as well.
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 cinderVelocity = -Vector2.UnitY.RotatedBy(Lerp(-0.51f, 0.51f, i / 3f)) * 4.25f;
                            Utilities.NewProjectileBetter(Projectile.Center, cinderVelocity, ModContent.ProjectileType<HolyCinder>(), ProvidenceBehaviorOverride.CinderDamage, 0f);
                        }
                    }

                    Projectile.Kill();
                }
            }

            // Fade in if none of the above conditions were met.
            else
            {
                Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Time, true);
                Projectile.scale = Projectile.Opacity;
            }

            Time++;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.75f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = !ProvidenceBehaviorOverride.IsEnraged ? ModContent.Request<Texture2D>(Texture).Value : ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/HolyBlastNight").Value;
            Utilities.DrawAfterimagesCentered(Projectile, Color.White with { A = 72 }, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            return false;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.36f ? null : false;
    }
}
