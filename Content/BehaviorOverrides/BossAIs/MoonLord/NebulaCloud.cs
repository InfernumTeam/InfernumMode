using CalamityMod;
using InfernumMode.Core.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class NebulaCloud : ModProjectile
    {
        public ref float LightPower => ref Projectile.ai[0];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/NebulaGas1";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Nebula");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 78;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.scale = 1.5f;
            Projectile.hide = true;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            // Decide scale and initial rotation on the first frame this projectile exists.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Main.rand.NextFloat(1f, 1.7f);
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.localAI[0] = 1f;
            }

            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
            Projectile.Opacity = Utils.GetLerpValue(300f, 275f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);
            Projectile.rotation += Projectile.velocity.X * 0.004f;
            Projectile.velocity *= 0.985f;
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
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity * 0.8f;

            Color[] nebulaPalette = new Color[]
            {
                new Color(248, 0, 255),
                new Color(187, 0, 255),
                new Color(58, 97, 255),
                new Color(68, 171, 255),
                new Color(0, 255, 127),
            };

            Color nebulaColor = CalamityUtils.MulticolorLerp((float)Math.Pow(Math.Sin(Projectile.identity / 10f) * 0.5f + 0.5f, 2.1), nebulaPalette);
            Color drawColor = Color.Lerp(nebulaColor, Color.White, 0.5f) * opacity;
            Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale * 0.9f;
            Main.spriteBatch.Draw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, scale * 1.5f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
