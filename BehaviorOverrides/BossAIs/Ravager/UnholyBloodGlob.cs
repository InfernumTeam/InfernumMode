using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class UnholyBloodGlob : ModProjectile
    {
        public const float Gravity = 0.24f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ungodly Blood");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = 42;
            projectile.height = 46;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
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
            if (projectile.position.Y > projectile.ai[1] - 48f)
                projectile.tileCollide = true;

            // Deal no damage and increment the variable used to kill the projectile.
            projectile.localAI[1]++;
            if (projectile.localAI[1] > 360f)
            {
                projectile.localAI[0] += 10f;
                projectile.damage = 0;
            }

            // Kill the projectile after it stops dealing damage.
            if (projectile.localAI[0] > 255f)
            {
                projectile.Kill();
                projectile.localAI[0] = 255f;
            }

            // Adjust projectile visibility based on the kill timer.
            projectile.alpha = (int)(100.0 + projectile.localAI[0] * 0.7);

            if (projectile.velocity.Y != 0f && projectile.ai[0] == 0f)
            {
                // Rotate based on velocity, only do this here, because it's falling.
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
                // Prevent sliding
                projectile.velocity.X = 0f;

                // Do not animate falling frames
                projectile.ai[0] = 1f;

                if (projectile.frame < 2)
                {
                    // Set frame to blob and frame counter to 0.
                    projectile.frame = 2;
                    projectile.frameCounter = 0;

                    // Play a squish sound.
                    Main.PlaySound(SoundID.NPCDeath21, projectile.Center);
                }

                projectile.rotation = 0f;
                projectile.gfxOffY = 4f;

                projectile.frameCounter++;
                if (projectile.frameCounter > 6)
                {
                    projectile.frame++;
                    projectile.frameCounter = 0;
                }
                if (projectile.frame > 5)
                    projectile.frame = 5;
            }

            // Do velocity code after the frame code, to avoid messing them up.
            // Stop falling if water or lava is hit
            if (projectile.wet || projectile.lavaWet)
                projectile.velocity.Y = 0f;
            else
            {
                // Fall.
                projectile.velocity.Y += Gravity;
                if (projectile.velocity.Y > 15.5f)
                    projectile.velocity.Y = 15.5f;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
            fallThrough = false;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(projectile.Center, 16f, targetHitbox);

        public override bool CanHitPlayer(Player target) => projectile.localAI[1] <= 900f && projectile.localAI[1] > 45f;

        public override Color? GetAlpha(Color lightColor)
        {
            Color c = Color.Red * projectile.Opacity;
            return c;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, frame, new Color(1f, 1f, 1f, 0f) * projectile.Opacity * 0.65f, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor);
            return false;
        }
    }
}
