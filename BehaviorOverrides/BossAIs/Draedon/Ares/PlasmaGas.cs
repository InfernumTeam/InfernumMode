using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class PlasmaGas : ModProjectile
    {
        public ref float LightPower => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Plasma");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 50;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.timeLeft = 180;
            projectile.scale = 1.5f;
            projectile.hide = true;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
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
            projectile.Opacity = Utils.InverseLerp(180f, 165f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 60f, projectile.timeLeft, true);
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
            Color drawColor = new Color(141, 255, 105) * opacity;
            Vector2 scale = projectile.Size / texture.Size() * projectile.scale * 1.35f;
            spriteBatch.Draw(texture, drawPosition, null, drawColor, projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
