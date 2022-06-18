using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordDeathBloodBlob : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Blob");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = 52;
            projectile.height = 56;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.scale = Main.rand?.NextFloat(0.7f, 1.3f) ?? 1f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (projectile.position.Y > projectile.ai[1] - 32f)
                projectile.tileCollide = true;

            projectile.localAI[1] += 1f;
            if (projectile.localAI[1] > 300f)
                projectile.localAI[0] += 10f;

            if (projectile.localAI[0] > 255f)
            {
                projectile.Kill();
                projectile.localAI[0] = 255f;
            }

            // Add light based on the current opacity.
            Lighting.AddLight(projectile.Center, Color.Turquoise.ToVector3() * projectile.Opacity);

            // Adjust projectile visibility based on the kill timer
            projectile.alpha = (int)(100.0 + projectile.localAI[0] * 0.7);

            if (projectile.velocity.Y != 0f && projectile.ai[0] == 0f)
            {
                // Rotate based on velocity, only do this when falling.
                projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

                projectile.frameCounter++;
                if (projectile.frameCounter > 6)
                {
                    projectile.frame++;
                    projectile.frameCounter = 0;
                }
                if (projectile.frame > 1)
                    projectile.frame = 0;
            }
            else
            {
                // Prevent sliding.
                projectile.velocity.X = 0f;

                // Do not animate falling frames.
                projectile.ai[0] = 1f;

                if (projectile.frame < 2)
                {
                    // Set frame to blob and frame counter to 0.
                    projectile.frame = 2;
                    projectile.frameCounter = 0;

                    // Play squish sound
                    Main.PlaySound(SoundID.NPCDeath21, projectile.Center);

                    // Emit dust
                    float scale = 1.6f;
                    float scale2 = 0.8f;
                    float scale3 = 2f;
                    Vector2 dustVelocity = (projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * projectile.velocity.Length();
                    for (int num53 = 0; num53 < 10; num53++)
                    {
                        Dust greenBlood = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 273, 0f, 0f, 200, default, scale);
                        greenBlood.position = projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * projectile.width / 2f;
                        greenBlood.noGravity = true;
                        greenBlood.velocity.Y -= 2f;
                        greenBlood.velocity *= 3f;
                        greenBlood.velocity += dustVelocity * Main.rand.NextFloat();

                        greenBlood = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 273, 0f, 0f, 100, default, scale2);
                        greenBlood.position = projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * projectile.width / 2f;
                        greenBlood.velocity.Y -= 2f;
                        greenBlood.velocity *= 2f;
                        greenBlood.noGravity = true;
                        greenBlood.fadeIn = 1f;
                        greenBlood.velocity += dustVelocity * Main.rand.NextFloat();
                    }
                    for (int num55 = 0; num55 < 5; num55++)
                    {
                        Dust greenBlood = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 273, 0f, 0f, 0, default, scale3);
                        greenBlood.position = projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(projectile.velocity.ToRotation()) * projectile.width / 3f;
                        greenBlood.noGravity = true;
                        greenBlood.velocity.Y -= 2f;
                        greenBlood.velocity *= 0.5f;
                        greenBlood.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
                    }
                }

                projectile.rotation = 0f;
                projectile.frameCounter++;
                if (projectile.frameCounter > 6)
                {
                    projectile.frame++;
                    projectile.frameCounter = 0;
                }
                if (projectile.frame > 5)
                    projectile.frame = 5;
            }

            // Stop falling if water or lava is hit.
            if (projectile.wet || projectile.lavaWet)
                projectile.velocity.Y = 0f;
            else
            {
                // Fall.
                projectile.velocity.Y += 0.4f;
                if (projectile.velocity.Y > 16f)
                    projectile.velocity.Y = 16f;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
            fallThrough = false;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override Color? GetAlpha(Color lightColor)
        {
            if (projectile.localAI[1] > 300f)
            {
                byte b2 = (byte)((26f - (projectile.localAI[1] - 300f)) * 10f);
                byte a2 = (byte)(projectile.alpha * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return new Color(255, 255, 255, projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor);
            return false;
        }
    }
}
