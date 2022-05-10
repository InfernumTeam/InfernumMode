using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class EssenceCleave : ModProjectile
    {
        public float LineWidth = 0f;
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
            projectile.timeLeft = 90;
            projectile.MaxUpdates = 2;
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft == 40f)
            {
                Vector2 sliceSpawnPosition = projectile.Center - projectile.ai[0].ToRotationVector2() * 3900f;
                Utilities.NewProjectileBetter(sliceSpawnPosition, projectile.ai[0].ToRotationVector2() * 6f, ModContent.ProjectileType<EssenceSlice>(), 450, 0f);

                sliceSpawnPosition = projectile.Center + projectile.ai[0].ToRotationVector2() * 3900f;
                Utilities.NewProjectileBetter(sliceSpawnPosition, projectile.ai[0].ToRotationVector2() * -6f, ModContent.ProjectileType<EssenceSlice>(), 450, 0f);
            }

            if (projectile.timeLeft < 40f)
                LineWidth -= 0.1f;
            else if (LineWidth < 5f)
                LineWidth += 0.3f;

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Vector2 offset = projectile.ai[0].ToRotationVector2() * 4000f;
            spriteBatch.DrawLineBetter(projectile.Center - offset, projectile.Center + offset, Color.Purple, LineWidth);
            spriteBatch.DrawLineBetter(projectile.Center - offset, projectile.Center + offset, Color.Magenta * 0.6f, LineWidth * 0.5f);

            spriteBatch.ResetBlendState();
            return true;
        }
    }
}

