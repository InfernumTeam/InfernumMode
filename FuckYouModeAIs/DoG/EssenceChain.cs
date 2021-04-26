using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.DoG
{
    public class EssenceChain : ModProjectile
    {
        public float LineWidth = 0f;
        public float OffsetFactor = -2800f;
        public ref float Time => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Chain");
        }

        public override void SetDefaults()
        {
            projectile.width = 90;
            projectile.height = 90;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            if (projectile.timeLeft < 40)
                LineWidth -= 0.1f;
            else if (LineWidth < 4f)
                LineWidth += 0.15f;

            Time++;
            if (Time > 90f && OffsetFactor < 2800f)
            {
                OffsetFactor += 360f;

                Vector2 offset = projectile.ai[0].ToRotationVector2() * OffsetFactor;
                Projectile.NewProjectileDirect(projectile.Center + offset, Vector2.Zero, ModContent.ProjectileType<EssenceExplosion>(), 90, 0f);
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Vector2 offset = projectile.ai[0].ToRotationVector2() * 6000f;
            spriteBatch.DrawLineBetter(projectile.Center - offset, projectile.Center + offset, Color.Purple, LineWidth);
            spriteBatch.DrawLineBetter(projectile.Center - offset, projectile.Center + offset, Color.Magenta * 0.6f, LineWidth * 0.5f);

            spriteBatch.ResetBlendState();
            return true;
        }
    }
}

