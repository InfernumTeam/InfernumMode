using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class NebulaVortex : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Vortex");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            Projectile.MaxUpdates = 2;
        }

        public override void AI()
        {
            Time++;
            if (Time <= 50f)
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    Dust vortexStuff = Main.dust[Dust.NewDust(Projectile.Center - dustDirection * 30f, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                    vortexStuff.noGravity = true;
                    vortexStuff.position = Projectile.Center - dustDirection * Main.rand.NextFloat(10, 21);
                    vortexStuff.velocity = dustDirection.RotatedBy(MathHelper.PiOver2) * 4f;
                    vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                    vortexStuff.fadeIn = 0.5f;
                }
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    Dust darkMatter = Main.dust[Dust.NewDust(Projectile.Center - dustDirection * 30f, 0, 0, 240, 0f, 0f, 0, default, 1f)];
                    darkMatter.noGravity = true;
                    darkMatter.position = Projectile.Center - dustDirection * 30f;
                    darkMatter.velocity = dustDirection.RotatedBy(-MathHelper.PiOver2) * 2f;
                    darkMatter.scale = 0.5f + Main.rand.NextFloat();
                    darkMatter.fadeIn = 0.5f;
                }
            }
            if (Time <= 90f)
            {
                Projectile.scale = (Time - 50f) / 40f;
                Projectile.alpha = 255 - (int)(255f * Projectile.scale);
                Projectile.rotation -= MathHelper.Pi / 20f;

                Vector2 shootDirection = Projectile.ai[1].ToRotationVector2();
                Vector2 fuck = shootDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(10f, 20f);
                shootDirection *= Main.rand.NextFloat(-80f, 80f);
                Vector2 dustVelocity = (shootDirection - fuck) * 0.1f;
                Dust vortexStuff = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                vortexStuff.noGravity = true;
                vortexStuff.position = Projectile.Center + fuck;
                vortexStuff.velocity = dustVelocity;
                vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                vortexStuff.fadeIn = 0.5f;
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    vortexStuff = Main.dust[Dust.NewDust(Projectile.Center - dustDirection * 30f, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                    vortexStuff.noGravity = true;
                    vortexStuff.position = Projectile.Center - dustDirection * Main.rand.NextFloat(10, 21);
                    vortexStuff.velocity = dustDirection.RotatedBy(MathHelper.PiOver2) * 6f;
                    vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                    vortexStuff.fadeIn = 0.5f;
                    vortexStuff.customData = Projectile.Center;
                }
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    Dust darkMatter = Main.dust[Dust.NewDust(Projectile.Center - dustDirection * 30f, 0, 0, 240, 0f, 0f, 0, default, 1f)];
                    darkMatter.noGravity = true;
                    darkMatter.position = Projectile.Center - dustDirection * 30f;
                    darkMatter.velocity = dustDirection.RotatedBy(-MathHelper.PiOver2) * 3f;
                    darkMatter.scale = 0.5f + Main.rand.NextFloat();
                    darkMatter.fadeIn = 0.5f;
                    darkMatter.customData = Projectile.Center;
                }

                if (Time == 90f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float boltOffsetAngle = Main.rand.NextBool() ? MathHelper.Pi / 10f : 0f;
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 boltShootVelocity = (MathHelper.TwoPi * i / 10f + boltOffsetAngle).ToRotationVector2() * 4f;
                        Utilities.NewProjectileBetter(Projectile.Center + boltShootVelocity * 4f, boltShootVelocity, ProjectileID.PhantasmalBolt, 215, 0f);
                    }
                }
                return;
            }
            if (Time > 120f)
            {
                Projectile.scale = 1f - (Time - 120f) / 60f;
                Projectile.alpha = 255 - (int)(255f * Projectile.scale);
                Projectile.rotation -= MathHelper.Pi / 30f;
                if (Projectile.alpha >= 255)
                    Projectile.Kill();

                return;
            }
            Projectile.scale = 1f;
            Projectile.alpha = 0;
            Projectile.rotation -= MathHelper.Pi / 60f;
            if (Main.rand.NextBool(2))
            {
                Vector2 dustOffset = Main.rand.NextVector2Unit();
                Dust vortexStuff = Main.dust[Dust.NewDust(Projectile.Center - dustOffset * 30f, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                vortexStuff.noGravity = true;
                vortexStuff.position = Projectile.Center - dustOffset * (float)Main.rand.Next(10, 21);
                vortexStuff.velocity = dustOffset.RotatedBy(MathHelper.PiOver2) * 6f;
                vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                vortexStuff.fadeIn = 0.5f;
                vortexStuff.customData = Projectile.Center;

                dustOffset = Main.rand.NextVector2Unit();
                Dust darkMatter = Main.dust[Dust.NewDust(Projectile.Center - dustOffset * 30f, 0, 0, 240, 0f, 0f, 0, default, 1f)];
                darkMatter.noGravity = true;
                darkMatter.position = Projectile.Center - dustOffset * 30f;
                darkMatter.velocity = dustOffset.RotatedBy(-MathHelper.PiOver2) * 3f;
                darkMatter.scale = 0.5f + Main.rand.NextFloat();
                darkMatter.fadeIn = 0.5f;
                darkMatter.customData = Projectile.Center;
            }
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D portalTexture = Utilities.ProjTexture(Projectile.type);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * Projectile.Opacity * 1.8f;
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, -Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Cyan portal.
            color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * Projectile.Opacity * 1.6f;
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation * 0.6f, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Magenta portal.
            color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * Projectile.Opacity * 1.6f;
            Main.spriteBatch.Draw(portalTexture, drawPosition, null, color, Projectile.rotation * -0.6f, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
