using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class SwervingSandShark : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float Variant => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Shark");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 44;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 480;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.15f, 0f, 1f);

            // Determine frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            // Wave up and down over time.
            Vector2 moveOffset = Vector2.UnitX * (float)Math.Sin(Time / 17f) * 5f;
            projectile.Center += moveOffset;

            projectile.rotation = (projectile.velocity + moveOffset).ToRotation();
            projectile.rotation += MathHelper.Pi;
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(4, Main.projFrames[projectile.type], (int)Variant % 4, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            spriteBatch.Draw(texture, drawPosition, frame, Color.White * projectile.Opacity, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
