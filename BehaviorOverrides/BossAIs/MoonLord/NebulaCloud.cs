using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class NebulaCloud : ModProjectile
    {
        public ref float LightPower => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Nebula");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 78;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.timeLeft = 300;
            projectile.scale = 1.5f;
            projectile.hide = true;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Decide scale and initial rotation on the first frame this projectile exists.
            if (projectile.localAI[0] == 0f)
            {
                projectile.scale = Main.rand.NextFloat(1f, 1.7f);
                projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.localAI[0] = 1f;
            }

            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
            projectile.Opacity = Utils.InverseLerp(300f, 275f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 60f, projectile.timeLeft, true);
            projectile.rotation += projectile.velocity.X * 0.004f;
            projectile.velocity *= 0.985f;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override bool CanDamage() => projectile.Opacity > 0.6f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 screenArea = new Vector2(Main.screenWidth, Main.screenHeight);
            Rectangle screenRectangle = Utils.CenteredRectangle(Main.screenPosition + screenArea * 0.5f, screenArea * 1.33f);

            if (!projectile.Hitbox.Intersects(screenRectangle))
                return false;

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            float opacity = Utils.InverseLerp(0f, 0.08f, LightPower, true) * projectile.Opacity * 0.8f;

            Color[] nebulaPalette = new Color[]
            {
                new Color(248, 0, 255),
                new Color(187, 0, 255),
                new Color(58, 97, 255),
                new Color(68, 171, 255),
                new Color(0, 255, 127),
            };

            Color nebulaColor = CalamityUtils.MulticolorLerp((float)Math.Pow(Math.Sin(projectile.identity / 10f) * 0.5f + 0.5f, 2.1), nebulaPalette);
            Color drawColor = Color.Lerp(nebulaColor, Color.White, 0.5f) * opacity;
            Vector2 scale = projectile.Size / texture.Size() * projectile.scale * 0.9f;

            for (int i = 0; i < 2; i++)
                spriteBatch.Draw(texture, drawPosition, null, drawColor, projectile.rotation, origin, scale * 1.5f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
