using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod
{
    public class GroundSlimeGlob : ModProjectile
    {
        public const float Gravity = 0.196f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Unstable Slime Glob");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 46;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
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
            if (Projectile.position.Y > Projectile.ai[1] - 48f)
                Projectile.tileCollide = true;

            // Deal no damage and increment the variable used to kill the projectile.
            Projectile.localAI[1]++;
            if (Projectile.localAI[1] > 180f)
            {
                Projectile.localAI[0] += 10f;
                Projectile.damage = 0;
            }

            // Kill the projectile after it stops dealing damage.
            if (Projectile.localAI[0] > 255f)
            {
                Projectile.Kill();
                Projectile.localAI[0] = 255f;
            }

            // Adjust projectile visibility based on the kill timer.
            Projectile.alpha = (int)(100.0 + Projectile.localAI[0] * 0.7);

            if (Projectile.velocity.Y != 0f && Projectile.ai[0] == 0f)
            {
                // Rotate based on velocity, only do this here, because it's falling.
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
                // Prevent sliding
                Projectile.velocity.X = 0f;

                // Do not animate falling frames
                Projectile.ai[0] = 1f;

                if (Projectile.frame < 2)
                {
                    // Set frame to blob and frame counter to 0.
                    Projectile.frame = 2;
                    Projectile.frameCounter = 0;

                    // Play a squish sound.
                    SoundEngine.PlaySound(SoundID.NPCDeath21, Projectile.Center);
                }

                Projectile.rotation = 0f;
                Projectile.gfxOffY = 4f;

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 6)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame > 5)
                    Projectile.frame = 5;
            }

            // Do velocity code after the frame code, to avoid messing them up.
            // Stop falling if water or lava is hit
            if (Projectile.wet || Projectile.lavaWet)
                Projectile.velocity.Y = 0f;
            else
            {
                // Fall.
                Projectile.velocity.Y += Gravity;
                if (Projectile.velocity.Y > 14.5f)
                    Projectile.velocity.Y = 14.5f;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 16f, targetHitbox);

        public override bool CanHitPlayer(Player target) => Projectile.localAI[1] is <= 900f and > 45f;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, new Color(1f, 1f, 1f, 0f) * Projectile.Opacity * 0.65f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
