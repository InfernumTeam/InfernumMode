using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class SmallElectricGasGloud : ModProjectile
    {
        public ref float LightPower => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Electric Cloud");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 80;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 500;
            Projectile.scale = 1.5f;
            Projectile.hide = true;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Decide scale and initial rotation on the first frame this projectile exists.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Main.rand.NextFloat(1f, 1.5f);
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.localAI[0] = 1f;
            }

            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
            Projectile.Opacity = Utils.GetLerpValue(500f, 475f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);
            Projectile.rotation += Projectile.velocity.X * 0.004f;
            Projectile.velocity *= 0.985f;

            // Release electric sparks.
            if (Main.rand.NextFloat() < (float)Math.Pow(Projectile.Opacity, 2D) * 0.05f)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f), 226);
                spark.velocity = Main.rand.NextVector2Circular(7f, 7f);
                spark.fadeIn = 0.7f;
                spark.scale *= 1.2f;
                spark.noGravity = true;
            }
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.6f;

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 screenArea = new(Main.screenWidth, Main.screenHeight);
            Rectangle screenRectangle = Utils.CenteredRectangle(Main.screenPosition + screenArea * 0.5f, screenArea * 1.33f);

            if (!Projectile.Hitbox.Intersects(screenRectangle))
                return false;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity;
            Color drawColor = Color.Lerp(Color.Cyan, Color.White, 0.5f) * opacity;
            Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale;

            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
