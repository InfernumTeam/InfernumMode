using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class TheMoon : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static bool MoonIsNotInSky => Utilities.AnyProjectiles(ModContent.ProjectileType<TheMoon>());
        
        public override void SetStaticDefaults() => DisplayName.SetDefault("The Moon");

        public override void SetDefaults()
        {
            Projectile.width = 942;
            Projectile.height = 942;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 72000;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        
        public override void AI()
        {
            Time++;

            // Slowly spin around.
            float angularVelocity = MathHelper.Clamp(Time / 240f, 0f, 1f) * MathHelper.Pi * 0.005f;
            Projectile.rotation += angularVelocity;
        }

        public override void Kill(int timeLeft)
        {
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 18f;
                drawOffset += new Vector2(0.707f, -0.707f) * 12f;

                Color rainbowColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f + i / 8f) % 1f, 1f, 0.55f) * 0.6f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, rainbowColor, Projectile.rotation * i / 8f, origin, Projectile.scale, 0, 0f);
            }

            Main.spriteBatch.EnterShaderRegion(BlendState.NonPremultiplied);
            Color color = Projectile.GetAlpha(Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.3f % 1f, 1f, 0.98f)) * 0.3f;
            color.A = 255;
            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
