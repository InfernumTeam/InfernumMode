using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class RefractionRotor : ModProjectile
    {
        public ref float TotalLasersToFire => ref projectile.ai[0];
        public ref float LaserShootOffsetAngle => ref projectile.ai[1];
        public Player Target => Main.player[Player.FindClosest(projectile.Center, 1, 1)];
        public float PointAtTargetInterpolant => Utils.InverseLerp(1720f, 2500f, projectile.Distance(Target.Center), true);
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Refraction Rotor");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 126;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.timeLeft = 90;
            projectile.Opacity = 0f;
            projectile.hide = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            // Play a spawn sound.
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item71, projectile.Center);
                projectile.localAI[0] = 1f;
            }

            // Initialize the shoot offset angle.
            if (Main.netMode != NetmodeID.MultiplayerClient && LaserShootOffsetAngle == 0f)
            {
                LaserShootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.netUpdate = true;
            }

            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
            projectile.rotation += (projectile.identity % 2 == 0).ToDirectionInt() * 0.3f;
            projectile.velocity *= 0.96f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Thanatos/RefractionRotorGlowmask");
            Vector2 origin = texture.Size() * 0.5f;
            float pulseInterpolant = Utils.InverseLerp(60f, 45f, projectile.timeLeft, true);
            Color pulseColor = Color.Lerp(Color.White, Color.Red, (float)Math.Sin(projectile.identity + Main.GlobalTime * 9.1f));
            pulseColor.A = 0;

            Color glowmaskColor = Color.Lerp(Color.White, pulseColor, pulseInterpolant);
            glowmaskColor.A = 0;

            // Draw laser telegraph lines.
            if (TotalLasersToFire > 0f && pulseInterpolant > 0f)
            {
                for (int i = 0; i < TotalLasersToFire; i++)
                {
                    // Define the telegraph direction.
                    // As the player moves away the lines focus on them more powerfully.
                    Vector2 telegraphDirection = (MathHelper.TwoPi * i / TotalLasersToFire + LaserShootOffsetAngle).ToRotationVector2();
                    Vector2 aimedOffset = (Utils.RandomNextSeed((ulong)(i + projectile.identity)) * MathHelper.E % MathHelper.TwoPi).ToRotationVector2() * 0.2f;
                    Vector2 aimedDirection = (projectile.SafeDirectionTo(Target.Center) + aimedOffset).SafeNormalize(Vector2.UnitY);
                    telegraphDirection = Vector2.Lerp(telegraphDirection, aimedDirection, PointAtTargetInterpolant);

                    // Determine telegraph line characteristics.
                    Vector2 start = projectile.Center;
                    Vector2 end = start + telegraphDirection * 1800f;
                    float telegraphWidth = pulseInterpolant * Utils.InverseLerp(0f, 6f, projectile.timeLeft, true) * 8f;
                    float telegraphColorInterpolant = ((float)Math.Sin(projectile.identity + telegraphDirection.ToRotation()) * 0.5f + 0.5f) * 0.65f;
                    Color telegraphColor = Color.Lerp(Color.Red, Color.Wheat, telegraphColorInterpolant) * pulseInterpolant * 0.6f;

                    // Use an exo color in the final phase.
                    if (TotalLasersToFire >= 7f)
                    {
                        Color exoColor = CalamityUtils.MulticolorLerp((i / TotalLasersToFire + projectile.identity * 0.318f) % 1f, CalamityUtils.ExoPalette) * 0.7f;
                        telegraphColor = Color.Lerp(telegraphColor, exoColor, 0.6f);
                    }
                    telegraphColor *= 0.7f;

                    // Draw an inner and outer telegraph line.
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                    telegraphColor.A = 0;
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth * 0.5f);
                }
            }

            float rotation = projectile.rotation;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            Color color = Color.White;
            color = Color.Lerp(color, glowmaskColor, Utils.InverseLerp(40f, 18f, projectile.timeLeft, true) * 0.4f);
            color.A = 255;
            Main.spriteBatch.Draw(texture, drawPosition, null, color * projectile.Opacity, rotation, origin, projectile.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, null, glowmaskColor * projectile.Opacity, rotation, origin, projectile.scale, 0, 0f);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaBolt"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (TotalLasersToFire > 0f)
            {
                for (int i = 0; i < TotalLasersToFire; i++)
                {
                    float laserShootSpeed = MathHelper.Lerp(21f, 35f, PointAtTargetInterpolant);
                    Vector2 laserDirection = (MathHelper.TwoPi * i / TotalLasersToFire + LaserShootOffsetAngle).ToRotationVector2();
                    Vector2 aimedOffset = (Utils.RandomNextSeed((ulong)(i + projectile.identity)) * MathHelper.E % MathHelper.TwoPi).ToRotationVector2() * 0.2f;
                    Vector2 aimedDirection = (projectile.SafeDirectionTo(Target.Center) + aimedOffset).SafeNormalize(Vector2.UnitY);
                    laserDirection = Vector2.Lerp(laserDirection, aimedDirection, PointAtTargetInterpolant);
                    Vector2 laserVelocity = laserDirection * laserShootSpeed;
                    int spark = Utilities.NewProjectileBetter(projectile.Center, laserVelocity, ModContent.ProjectileType<ExolaserSpark>(), 500, 0f);
                    if (Main.projectile.IndexInRange(spark))
                        Main.projectile[spark].MaxUpdates = 3;
                }
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers)
        {
            behindProjectiles.Add(index);
        }
    }
}
