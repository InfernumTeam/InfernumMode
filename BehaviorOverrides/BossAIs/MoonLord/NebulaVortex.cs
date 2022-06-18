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
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Vortex");

        public override void SetDefaults()
        {
            projectile.width = 90;
            projectile.height = 90;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.hide = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
            projectile.MaxUpdates = 2;
        }

        public override void AI()
        {
            Time++;
            if (Time <= 50f)
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    Dust vortexStuff = Main.dust[Dust.NewDust(projectile.Center - dustDirection * 30f, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                    vortexStuff.noGravity = true;
                    vortexStuff.position = projectile.Center - dustDirection * Main.rand.NextFloat(10, 21);
                    vortexStuff.velocity = dustDirection.RotatedBy(MathHelper.PiOver2) * 4f;
                    vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                    vortexStuff.fadeIn = 0.5f;
                }
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    Dust darkMatter = Main.dust[Dust.NewDust(projectile.Center - dustDirection * 30f, 0, 0, 240, 0f, 0f, 0, default, 1f)];
                    darkMatter.noGravity = true;
                    darkMatter.position = projectile.Center - dustDirection * 30f;
                    darkMatter.velocity = dustDirection.RotatedBy(-MathHelper.PiOver2) * 2f;
                    darkMatter.scale = 0.5f + Main.rand.NextFloat();
                    darkMatter.fadeIn = 0.5f;
                }
            }
            if (Time <= 90f)
            {
                projectile.scale = (Time - 50f) / 40f;
                projectile.alpha = 255 - (int)(255f * projectile.scale);
                projectile.rotation -= MathHelper.Pi / 20f;

                Vector2 shootDirection = projectile.ai[1].ToRotationVector2();
                Vector2 fuck = shootDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(10f, 20f);
                shootDirection *= Main.rand.NextFloat(-80f, 80f);
                Vector2 dustVelocity = (shootDirection - fuck) * 0.1f;
                Dust vortexStuff = Main.dust[Dust.NewDust(projectile.Center, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                vortexStuff.noGravity = true;
                vortexStuff.position = projectile.Center + fuck;
                vortexStuff.velocity = dustVelocity;
                vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                vortexStuff.fadeIn = 0.5f;
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    vortexStuff = Main.dust[Dust.NewDust(projectile.Center - dustDirection * 30f, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                    vortexStuff.noGravity = true;
                    vortexStuff.position = projectile.Center - dustDirection * Main.rand.NextFloat(10, 21);
                    vortexStuff.velocity = dustDirection.RotatedBy(MathHelper.PiOver2) * 6f;
                    vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                    vortexStuff.fadeIn = 0.5f;
                    vortexStuff.customData = projectile.Center;
                }
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustDirection = Main.rand.NextVector2Unit();
                    Dust darkMatter = Main.dust[Dust.NewDust(projectile.Center - dustDirection * 30f, 0, 0, 240, 0f, 0f, 0, default, 1f)];
                    darkMatter.noGravity = true;
                    darkMatter.position = projectile.Center - dustDirection * 30f;
                    darkMatter.velocity = dustDirection.RotatedBy(-MathHelper.PiOver2) * 3f;
                    darkMatter.scale = 0.5f + Main.rand.NextFloat();
                    darkMatter.fadeIn = 0.5f;
                    darkMatter.customData = projectile.Center;
                }

                if (Time == 90f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float boltOffsetAngle = Main.rand.NextBool() ? MathHelper.Pi / 10f : 0f;
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 boltShootVelocity = (MathHelper.TwoPi * i / 10f + boltOffsetAngle).ToRotationVector2() * 4f;
                        Utilities.NewProjectileBetter(projectile.Center + boltShootVelocity * 4f, boltShootVelocity, ProjectileID.PhantasmalBolt, 215, 0f);
                    }
                }
                return;
            }
            if (Time > 120f)
            {
                projectile.scale = 1f - (Time - 120f) / 60f;
                projectile.alpha = 255 - (int)(255f * projectile.scale);
                projectile.rotation -= MathHelper.Pi / 30f;
                if (projectile.alpha >= 255)
                    projectile.Kill();

                return;
            }
            projectile.scale = 1f;
            projectile.alpha = 0;
            projectile.rotation -= MathHelper.Pi / 60f;
            if (Main.rand.NextBool(2))
            {
                Vector2 dustOffset = Main.rand.NextVector2Unit();
                Dust vortexStuff = Main.dust[Dust.NewDust(projectile.Center - dustOffset * 30f, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                vortexStuff.noGravity = true;
                vortexStuff.position = projectile.Center - dustOffset * (float)Main.rand.Next(10, 21);
                vortexStuff.velocity = dustOffset.RotatedBy(MathHelper.PiOver2) * 6f;
                vortexStuff.scale = 0.5f + Main.rand.NextFloat();
                vortexStuff.fadeIn = 0.5f;
                vortexStuff.customData = projectile.Center;

                dustOffset = Main.rand.NextVector2Unit();
                Dust darkMatter = Main.dust[Dust.NewDust(projectile.Center - dustOffset * 30f, 0, 0, 240, 0f, 0f, 0, default, 1f)];
                darkMatter.noGravity = true;
                darkMatter.position = projectile.Center - dustOffset * 30f;
                darkMatter.velocity = dustOffset.RotatedBy(-MathHelper.PiOver2) * 3f;
                darkMatter.scale = 0.5f + Main.rand.NextFloat();
                darkMatter.fadeIn = 0.5f;
                darkMatter.customData = projectile.Center;
            }
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D portalTexture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * projectile.Opacity * 1.8f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(portalTexture, drawPosition, null, color, -projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Cyan portal.
            color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * 0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Magenta portal.
            color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * -0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
