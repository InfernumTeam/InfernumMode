using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class EssenceSliceTelegraphLine : ModProjectile
    {
        public float LineWidth = 0f;
        public ref float Time => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Chain");
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 64;
            Projectile.MaxUpdates = 3;
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft == 22f)
            {
                Vector2 sliceSpawnPosition = Projectile.Center - Projectile.ai[0].ToRotationVector2() * 3900f;
                Utilities.NewProjectileBetter(sliceSpawnPosition, Projectile.ai[0].ToRotationVector2() * 6f, ModContent.ProjectileType<EssenceSlice>(), 450, 0f);

                sliceSpawnPosition = Projectile.Center + Projectile.ai[0].ToRotationVector2() * 3900f;
                Utilities.NewProjectileBetter(sliceSpawnPosition, Projectile.ai[0].ToRotationVector2() * -6f, ModContent.ProjectileType<EssenceSlice>(), 450, 0f);
            }

            if (Projectile.timeLeft < 22f)
                LineWidth -= 0.1f;
            else if (LineWidth < 5f)
                LineWidth += 0.3f;

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 offset = Projectile.ai[0].ToRotationVector2() * 4000f;
            Main.spriteBatch.DrawLineBetter(Projectile.Center - offset, Projectile.Center + offset, Color.DeepSkyBlue, LineWidth);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - offset, Projectile.Center + offset, Color.Cyan * 0.6f, LineWidth * 0.5f);
            return true;
        }
    }
}

