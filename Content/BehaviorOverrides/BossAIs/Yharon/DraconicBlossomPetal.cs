using CalamityMod.DataStructures;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon
{
    public class DraconicBlossomPetal : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public static int LifetimeExtensionInWater => 600;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Draconic Blossom Petal");
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.Opacity = 0f;
            Projectile.scale = Main.rand?.NextFloat(0.3f, 0.9f) ?? 0.6f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Pick a frame and initial lifetime.
            if (Projectile.localAI[0] == 0f)
            {
                float lifetimeInterpolant = Pow(Main.rand.NextFloat(), 0.6f);
                Lifetime = (int)Lerp(240f, 480f, lifetimeInterpolant);
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;
            }

            // Increase the lifetime if the blossom isn't fading out and lands on water.
            if (Collision.WetCollision(Projectile.TopLeft, Projectile.width, Projectile.height))
            {
                Projectile.velocity.X *= 0.96f;
                Projectile.velocity.Y = Lerp(Projectile.velocity.Y, -2f, 0.09f);
                if (Projectile.Opacity >= 0.8f && Lifetime <= LifetimeExtensionInWater)
                {
                    Lifetime += LifetimeExtensionInWater;
                    Projectile.netUpdate = true;
                }
            }

            // Slowly fall down.
            float maxSpeed = Lerp(0.7f, 5.4f, Projectile.scale);
            Projectile.velocity.X = Lerp(Projectile.velocity.X, Sign(Projectile.velocity.X) * maxSpeed * 1.2f, 0.006f);
            Projectile.velocity.Y = Lerp(Projectile.velocity.Y + 0.02f, maxSpeed, 0.05f);

            // Rotate.
            Projectile.rotation += Projectile.velocity.Y * 0.01f;

            // Handle fade effects.
            float fadeIn = Utils.GetLerpValue(5f, 25f, Time, true);
            float fadeOut = Utils.GetLerpValue(0f, 54f, Lifetime - Time, true);
            Projectile.Opacity = fadeIn * fadeOut;

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        // Prevent instant death upon touching tiles if the blossom has existed for a brief enough period of time.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Time <= 20f)
                Projectile.Kill();

            Projectile.velocity.X = 0f;
            return Time <= 4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D circle = InfernumTextureRegistry.LaserCircle.Value;

            float backglowOpacity = Projectile.Opacity * 0.3f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float backglowScale = Lerp(0.85f, 1.15f, Cos(Projectile.identity * 7f + Main.GlobalTimeWrappedHourly * 1.6f) * 0.5f + 0.5f) * Projectile.scale * Projectile.Opacity * 0.45f;
            Color backglowColor = Color.Lerp(Color.Red, Color.Pink, Projectile.Opacity) * backglowOpacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(circle, drawPosition, null, backglowColor, 0f, circle.Size() * 0.5f, backglowScale, 0, 0f);
            Main.spriteBatch.Draw(circle, drawPosition, null, Color.Magenta * backglowOpacity * 1.1f, 0f, circle.Size() * 0.5f, backglowScale * 0.75f, 0, 0f);
            Main.spriteBatch.ResetBlendState();

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition2 = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition2, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
