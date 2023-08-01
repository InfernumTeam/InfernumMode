using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class StolenCelestialObject : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public static bool MoonIsNotInSky => Utilities.AnyProjectiles(ModContent.ProjectileType<StolenCelestialObject>()) && !Main.dayTime;

        public static bool SunIsNotInSky => Utilities.AnyProjectiles(ModContent.ProjectileType<StolenCelestialObject>()) && Main.dayTime;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Big Stolen Celestial Object");

        public override void SetDefaults()
        {
            Projectile.width = 942;
            Projectile.height = 942;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 72000;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }

        public override void AI()
        {
            // Disappear if the empress is not present.
            if (!NPC.AnyNPCs(NPCID.HallowBoss))
                Projectile.Kill();

            if (Projectile.timeLeft < 90)
                Projectile.damage = 0;

            Time++;

            // Slowly spin around.
            float angularVelocity = Clamp(Time / 240f, 0f, 1f) * Pi * 0.005f;
            Projectile.rotation += angularVelocity;
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * 0.44f, targetHitbox);
        }

        public float SunWidthFunction(float completionRatio) => Projectile.width * Sin(Pi * completionRatio);

        public Color SunColorFunction(float completionRatio)
        {
            Color result = Color.Lerp(Color.Red, Color.Yellow, 0.3f);
            return result * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            DrawBloomFlare();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Pretend that the moon is in fact a sun during the daytime.
            if (Main.dayTime)
                DrawSun();
            else
                DrawMoon();

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        public void DrawBloomFlare()
        {
            Texture2D bloomFlare = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/BloomFlare").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.2f % 1f, 1f, 0.55f), 0.7f);
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 0.93f;
            float bloomFlareScale = Projectile.scale * 3f;
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, -bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);

            bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + 0.5f) % 1f, 1f, 0.55f), 0.7f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);
        }

        public void DrawMoon()
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/EmpressOfLight/TheMoon").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Color moonColor = Projectile.GetAlpha(Color.Wheat);
            moonColor = Color.Lerp(moonColor, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.2f % 1f, 1f, 0.8f), 0.7f) * 0.9f;
            moonColor.A = 127;

            Main.spriteBatch.Draw(texture, drawPosition, null, moonColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
        }

        public void DrawSun()
        {
            Texture2D invis = InfernumTextureRegistry.Invisible.Value;
            Texture2D noise = InfernumTextureRegistry.HarshNoise.Value;
            Effect fireball = InfernumEffectsRegistry.FireballShader.GetShader().Shader;

            fireball.Parameters["sampleTexture2"].SetValue(noise);
            fireball.Parameters["mainColor"].SetValue(Color.Lerp(Color.Red, Color.Yellow, 0.3f).ToVector3() * Projectile.Opacity);
            fireball.Parameters["resolution"].SetValue(new Vector2(250f, 250f));
            fireball.Parameters["speed"].SetValue(0.76f);
            fireball.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            fireball.Parameters["zoom"].SetValue(0.0004f);
            fireball.Parameters["dist"].SetValue(60f);
            fireball.Parameters["opacity"].SetValue(Projectile.Opacity);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            fireball.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(invis, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, invis.Size() * 0.5f, Projectile.width * 1.3f * Projectile.scale, SpriteEffects.None, 0f);
            if (!InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                fireball.Parameters["mainColor"].SetValue(Color.Lerp(Color.Red, Color.OrangeRed, 0.6f).ToVector3() * Projectile.Opacity);
                fireball.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(invis, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, invis.Size() * 0.5f, Projectile.width * 1.05f * Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
