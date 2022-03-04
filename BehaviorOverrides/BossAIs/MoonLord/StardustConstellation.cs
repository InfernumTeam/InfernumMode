using CalamityMod.Dusts;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class StardustConstellation : ModProjectile
    {
        public ref float Index => ref projectile.ai[0];
        public ref float Time => ref projectile.localAI[1];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Star");

        public override void SetDefaults()
        {
            projectile.scale = Main.rand?.NextFloat(0.8f, 1f) ?? 1f;
            projectile.width = projectile.height = (int)(projectile.scale * 64f);
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.hide = true;
            projectile.timeLeft = 90000;
        }

        public override void AI()
        {
            if (projectile.timeLeft < 60)
                projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 0.002f, 0.1f);

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Projectile projectileToConnectTo = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != projectile.type || !Main.projectile[i].active ||
                    Main.projectile[i].timeLeft < 25f || Main.projectile[i].ai[0] != Index - 1f ||
                    Main.projectile[i].ai[1] != projectile.ai[1])
                {
                    continue;
                }

                projectileToConnectTo = Main.projectile[i];
                break;
            }

            float fadeToOrange = Utils.InverseLerp(50f, 0f, projectile.timeLeft, true);
            Color stardustColor = new Color(0, 213, 255);
            Color solarColor = new Color(255, 140, 0);
            Color starColor = Color.Lerp(stardustColor, solarColor, fadeToOrange);

            Texture2D starTexture = Main.projectileTexture[projectile.type];
            float scaleFactor = Utils.InverseLerp(0f, 15f, Time, true) + Utils.InverseLerp(30f, 0f, projectile.timeLeft, true) * 2f;

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            for (int i = 0; i < 16; i++)
            {
                float drawOffsetFactor = ((float)Math.Cos(Main.GlobalTime * 40f) * 0.5f + 0.5f) * scaleFactor * fadeToOrange * 8f + 1f;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * drawOffsetFactor;
                spriteBatch.Draw(starTexture, drawPosition + drawOffset, null, starColor * 0.4f, 0f, starTexture.Size() * 0.5f, projectile.scale * scaleFactor, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(starTexture, drawPosition, null, starColor * 4f, 0f, starTexture.Size() * 0.5f, projectile.scale * scaleFactor, SpriteEffects.None, 0f);

            if (projectileToConnectTo != null)
            {
                Texture2D lineTexture = Main.extraTexture[47];
                Vector2 connectionDirection = projectile.SafeDirectionTo(projectileToConnectTo.Center);
                Vector2 start = projectile.Center + connectionDirection * projectile.scale * 24f;
                Vector2 end = projectileToConnectTo.Center - connectionDirection * projectile.scale * 24f;
                Vector2 scale = new Vector2(scaleFactor * 1.5f, (start - end).Length() / lineTexture.Height);
                Vector2 origin = new Vector2(lineTexture.Width * 0.5f, 0f);
                Color drawColor = Color.White;
                float rotation = (end - start).ToRotation() - MathHelper.PiOver2;

                spriteBatch.Draw(lineTexture, start - Main.screenPosition, null, drawColor, rotation, origin, scale, SpriteEffects.None, 0f);
            }

            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item91, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 initialVelocity = Vector2.UnitY * 6.5f;
            if (projectile.identity % 2f == 1f)
                initialVelocity = initialVelocity.RotatedBy(MathHelper.PiOver2);

            Utilities.NewProjectileBetter(projectile.Center, -initialVelocity, ProjectileID.CultistBossFireBall, 215, 0f);
            Utilities.NewProjectileBetter(projectile.Center, initialVelocity, ProjectileID.CultistBossFireBall, 215, 0f);
            Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
        }
    }
}
