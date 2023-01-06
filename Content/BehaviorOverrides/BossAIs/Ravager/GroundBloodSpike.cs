using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class GroundBloodSpike : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood Spike");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = Main.rand.NextFloat(1.1f, 1.6f);
                Projectile.netUpdate = true;
            }

            int fadeInTime = 6;
            int fadeoutTime = 10;
            int lifetime = 26;
            int maxValue = 5;
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.frame = Main.rand.Next(maxValue);
                for (int i = 0; i < 5; i++)
                {
                    Dust bloodDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), 5, Projectile.velocity * Main.rand.NextFloat(0.15f, 0.525f));
                    bloodDust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                    bloodDust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
                }
                for (int j = 0; j < 5; j++)
                {
                    Dust bloodDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), 5, Main.rand.NextVector2Circular(2f, 2f) + Projectile.velocity * Main.rand.NextFloat(0.15f, 0.375f));
                    bloodDust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                    bloodDust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
                    bloodDust.fadeIn = 1f;
                }
            }
            if (Time < fadeInTime)
            {
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.2f, 0f, 1f);
                Projectile.scale = Projectile.Opacity * Projectile.ai[1];
            }

            if (Time >= lifetime - fadeoutTime)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.2f, 0f, 1f);

            if (Time >= lifetime)
                Projectile.Kill();

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.Black, 0.36f) * Projectile.Opacity;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Main.spriteBatch.Draw(tex, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * Projectile.scale * 100f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Projectile.scale * 22f, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }
    }
}
