using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class SwervingSandShark : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float Variant => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Shark");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 44;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.15f, 0f, 1f);

            // Determine frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Wave up and down over time.
            Vector2 moveOffset = Vector2.UnitX * (float)Math.Sin(Time / 17f) * 5f;
            Projectile.Center += moveOffset;

            Projectile.rotation = (Projectile.velocity + moveOffset).ToRotation();
            Projectile.rotation += MathHelper.Pi;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(4, Main.projFrames[Projectile.type], (int)Variant % 4, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
