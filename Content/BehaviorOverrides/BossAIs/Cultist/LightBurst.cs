using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class LightBurst : ModProjectile
    {
        public ref float ActionCountdown => ref Projectile.ai[0];
        public ref float ExplosionTelegraphFade => ref Projectile.ai[1];
        public static readonly Vector2 TelegraphRingScale = new(0.8f, 1.333f);
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Light Burst");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = 45;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            ExplosionTelegraphFade++;

            // Wait for a bit before doing anything.
            if (ActionCountdown > 0f)
            {
                // Release some white dust near the rim.
                if (ActionCountdown % 8f == 7f)
                {
                    Vector2 dustSpawnOffset = Main.rand.NextVector2CircularEdge(TelegraphRingScale.X, TelegraphRingScale.Y) * Main.rand.NextFloat(84f, 96f) * 1.45f;
                    Dust light = Dust.NewDustPerfect(Projectile.Center + dustSpawnOffset, 267);
                    light.color = Color.White * 1.3f;
                    light.color.A = 0;
                    light.scale = Main.rand.NextFloat(1.7f, 2f);
                    light.fadeIn = 1.15f;
                    light.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    light.noGravity = true;
                }

                ActionCountdown--;
                Projectile.timeLeft = 45;
                return;
            }

            // Play an explosion sound.
            if (Projectile.timeLeft == 25f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft);
            Projectile.scale = SmoothStep(0.05f, 5f + Projectile.identity % 4f / 4f * 0.4f, Utils.GetLerpValue(45f, 5f, Projectile.timeLeft));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (ActionCountdown > 0f)
                return false;

            Rectangle hitbox = Utils.CenteredRectangle(Projectile.Center, new Vector2(0.6f, 1f) * Projectile.scale * 80f);
            return targetHitbox.Intersects(hitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D telegraphTexture = TextureAssets.Projectile[Projectile.type].Value;

            // Draw a single ring to show where the explosion will happen.
            if (ActionCountdown > 0f)
            {
                Color ringColor = Color.White * 0.425f;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, ringColor, 0f, telegraphTexture.Size() * 0.5f, TelegraphRingScale * 1.6f, SpriteEffects.None, 0f);

                float explosionTelegraphFade = Utils.GetLerpValue(0f, 20f, ExplosionTelegraphFade, true) * 0.3f;
                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, ringColor * explosionTelegraphFade, 0f, telegraphTexture.Size() * 0.5f, TelegraphRingScale * 5f, SpriteEffects.None, 0f);
                return false;
            }

            // Create a much fainter ring to act as a telegraph that shows where the explosion will happen.
            Color telegraphColor = Color.White * Projectile.Opacity * 0.2f;
            telegraphColor.A = 0;

            for (int i = 0; i < 35; i++)
            {
                Vector2 drawPosition = Projectile.Center + (TwoPi * i / 35f + Main.GlobalTimeWrappedHourly * 3f).ToRotationVector2();
                drawPosition -= Main.screenPosition;

                Vector2 scale = new Vector2(0.6f, 1f) * Projectile.scale;
                scale *= Lerp(0.015f, 1f, i / 35f);

                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, 0f, telegraphTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }



        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                Dust magic = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 267);
                magic.color = Color.SkyBlue;
                magic.scale = 1.1f;
                magic.fadeIn = 1.6f;
                magic.velocity = Main.rand.NextVector2Circular(2f, 2f);
                magic.velocity = Vector2.Lerp(magic.velocity, -Vector2.UnitY * magic.velocity.Length(), Main.rand.NextFloat(0.65f, 1f));
                magic.noGravity = true;
            }
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
