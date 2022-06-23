using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordDeathBloodBlob : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Blob");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 56;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.scale = Main.rand?.NextFloat(0.7f, 1.3f) ?? 1f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (Projectile.position.Y > Projectile.ai[1] - 32f)
                Projectile.tileCollide = true;

            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] > 300f)
                Projectile.localAI[0] += 10f;

            if (Projectile.localAI[0] > 255f)
            {
                Projectile.Kill();
                Projectile.localAI[0] = 255f;
            }

            // Add light based on the current opacity.
            Lighting.AddLight(Projectile.Center, Color.Turquoise.ToVector3() * Projectile.Opacity);

            // Adjust projectile visibility based on the kill timer
            Projectile.alpha = (int)(100.0 + Projectile.localAI[0] * 0.7);

            if (Projectile.velocity.Y != 0f && Projectile.ai[0] == 0f)
            {
                // Rotate based on velocity, only do this when falling.
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 6)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame > 1)
                    Projectile.frame = 0;
            }
            else
            {
                // Prevent sliding.
                Projectile.velocity.X = 0f;

                // Do not animate falling frames.
                Projectile.ai[0] = 1f;

                if (Projectile.frame < 2)
                {
                    // Set frame to blob and frame counter to 0.
                    Projectile.frame = 2;
                    Projectile.frameCounter = 0;

                    // Play squish sound
                    SoundEngine.PlaySound(SoundID.NPCDeath21, Projectile.Center);

                    // Emit dust
                    float scale = 1.6f;
                    float scale2 = 0.8f;
                    float scale3 = 2f;
                    Vector2 dustVelocity = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.velocity.Length();
                    for (int num53 = 0; num53 < 10; num53++)
                    {
                        Dust greenBlood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 273, 0f, 0f, 200, default, scale);
                        greenBlood.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                        greenBlood.noGravity = true;
                        greenBlood.velocity.Y -= 2f;
                        greenBlood.velocity *= 3f;
                        greenBlood.velocity += dustVelocity * Main.rand.NextFloat();

                        greenBlood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 273, 0f, 0f, 100, default, scale2);
                        greenBlood.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                        greenBlood.velocity.Y -= 2f;
                        greenBlood.velocity *= 2f;
                        greenBlood.noGravity = true;
                        greenBlood.fadeIn = 1f;
                        greenBlood.velocity += dustVelocity * Main.rand.NextFloat();
                    }
                    for (int num55 = 0; num55 < 5; num55++)
                    {
                        Dust greenBlood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 273, 0f, 0f, 0, default, scale3);
                        greenBlood.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                        greenBlood.noGravity = true;
                        greenBlood.velocity.Y -= 2f;
                        greenBlood.velocity *= 0.5f;
                        greenBlood.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
                    }
                }

                Projectile.rotation = 0f;
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 6)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame > 5)
                    Projectile.frame = 5;
            }

            // Stop falling if water or lava is hit.
            if (Projectile.wet || Projectile.lavaWet)
                Projectile.velocity.Y = 0f;
            else
            {
                // Fall.
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 16f)
                    Projectile.velocity.Y = 16f;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.localAI[1] > 300f)
            {
                byte b2 = (byte)((26f - (Projectile.localAI[1] - 300f)) * 10f);
                byte a2 = (byte)(Projectile.alpha * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
