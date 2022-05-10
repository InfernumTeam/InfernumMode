using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class LightBurst : ModProjectile
    {
        public ref float ActionCountdown => ref projectile.ai[0];
        public ref float ExplosionTelegraphFade => ref projectile.ai[1];
        public static readonly Vector2 TelegraphRingScale = new Vector2(0.8f, 1.333f);
        public override void SetStaticDefaults() => DisplayName.SetDefault("Light Burst");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.hide = true;
            projectile.timeLeft = 45;
            projectile.penetrate = -1;
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
                    Dust light = Dust.NewDustPerfect(projectile.Center + dustSpawnOffset, 267);
                    light.color = Color.White * 1.3f;
                    light.color.A = 0;
                    light.scale = Main.rand.NextFloat(1.7f, 2f);
                    light.fadeIn = 1.15f;
                    light.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    light.noGravity = true;
                }

                ActionCountdown--;
                projectile.timeLeft = 45;
                return;
            }

            // Play an explosion sound.
            if (projectile.timeLeft == 25f)
            {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item122, projectile.Center);
            }

            projectile.Opacity = Utils.InverseLerp(0f, 12f, projectile.timeLeft);
            projectile.scale = MathHelper.SmoothStep(0.05f, 5f + projectile.identity % 4f / 4f * 0.4f, Utils.InverseLerp(45f, 5f, projectile.timeLeft));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (ActionCountdown > 0f)
                return false;

            Rectangle hitbox = Utils.CenteredRectangle(projectile.Center, new Vector2(0.6f, 1f) * projectile.scale * 80f);
            return targetHitbox.Intersects(hitbox);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D telegraphTexture = Main.projectileTexture[projectile.type];

            // Draw a single ring to show where the explosion will happen.
            if (ActionCountdown > 0f)
            {
                Color ringColor = Color.White * 0.425f;
                Vector2 drawPosition = projectile.Center - Main.screenPosition;
                spriteBatch.Draw(telegraphTexture, drawPosition, null, ringColor, 0f, telegraphTexture.Size() * 0.5f, TelegraphRingScale * 1.6f, SpriteEffects.None, 0f);

                float explosionTelegraphFade = Utils.InverseLerp(0f, 20f, ExplosionTelegraphFade, true) * 0.16f;
                spriteBatch.Draw(telegraphTexture, drawPosition, null, ringColor * explosionTelegraphFade, 0f, telegraphTexture.Size() * 0.5f, TelegraphRingScale * 5f, SpriteEffects.None, 0f);
                return false;
            }

            // Create a much fainter ring to act as a telegraph that shows where the explosion will happen.
            Color telegraphColor = Color.White * projectile.Opacity * 0.2f;
            telegraphColor.A = 0;

            for (int i = 0; i < 35; i++)
            {
                Vector2 drawPosition = projectile.Center + (MathHelper.TwoPi * i / 35f + Main.GlobalTime * 3f).ToRotationVector2();
                drawPosition -= Main.screenPosition;

                Vector2 scale = new Vector2(0.6f, 1f) * projectile.scale;
                scale *= MathHelper.Lerp(0.015f, 1f, i / 35f);

                spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, 0f, telegraphTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                Dust magic = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 267);
                magic.color = Color.SkyBlue;
                magic.scale = 1.1f;
                magic.fadeIn = 1.6f;
                magic.velocity = Main.rand.NextVector2Circular(2f, 2f);
                magic.velocity = Vector2.Lerp(magic.velocity, -Vector2.UnitY * magic.velocity.Length(), Main.rand.NextFloat(0.65f, 1f));
                magic.noGravity = true;
            }
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
