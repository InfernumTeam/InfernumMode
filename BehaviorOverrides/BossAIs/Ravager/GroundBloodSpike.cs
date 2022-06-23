using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class GroundBloodSpike : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood Spike");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.Opacity = 0f;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.hide = true;
        }

        public override void AI()
        {
            if (projectile.ai[1] == 0f)
            {
                projectile.ai[1] = Main.rand.NextFloat(1.1f, 1.6f);
                projectile.netUpdate = true;
            }

            int fadeInTime = 6;
            int fadeoutTime = 10;
            int lifetime = 26;
            int maxValue = 5;
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
                projectile.rotation = projectile.velocity.ToRotation();
                projectile.frame = Main.rand.Next(maxValue);
                for (int i = 0; i < 5; i++)
                {
                    Dust bloodDust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(24f, 24f), 5, projectile.velocity * Main.rand.NextFloat(0.15f, 0.525f));
                    bloodDust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                    bloodDust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
                }
                for (int j = 0; j < 5; j++)
                {
                    Dust bloodDust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(24f, 24f), 5, Main.rand.NextVector2Circular(2f, 2f) + projectile.velocity * Main.rand.NextFloat(0.15f, 0.375f));
                    bloodDust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                    bloodDust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
                    bloodDust.fadeIn = 1f;
                }
            }
            if (Time < fadeInTime)
            {
                projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.2f, 0f, 1f);
                projectile.scale = projectile.Opacity * projectile.ai[1];
            }

            if (Time >= lifetime - fadeoutTime)
                projectile.Opacity = MathHelper.Clamp(projectile.Opacity - 0.2f, 0f, 1f);

            if (Time >= lifetime)
                projectile.Kill();

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.Black, 0.36f) * projectile.Opacity;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tex = ModContent.GetTexture(Texture);
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = tex.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Main.spriteBatch.Draw(tex, drawPosition, frame, projectile.GetAlpha(Color.White), projectile.rotation, frame.Size() * 0.5f, projectile.scale, 0, 0f);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 end = projectile.Center + projectile.velocity.SafeNormalize(-Vector2.UnitY) * projectile.scale * 100f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center, end, projectile.scale * 22f, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }
    }
}
