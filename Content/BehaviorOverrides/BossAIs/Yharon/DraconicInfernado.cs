using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon
{
    public class DraconicInfernado : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy TornadoDrawer
        {
            get;
            set;
        }

        public const int Lifetime = 540;

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Draconic Infernado");
        }

        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 1560;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.hostile = true;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Correct the position on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.Bottom = Projectile.Center;
                Projectile.localAI[0] = 1f;
            }

            // Calculate the opacity of the tornado. At the start and end of its life it will fade in/out.
            Projectile.Opacity = Utils.GetLerpValue(0, 35f, Time, true) * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);

            Time++;
        }

        public float TornadoWidthFunction(float completionRatio)
        {
            float scale = MathHelper.Lerp(0.04f, 1f, MathF.Pow(completionRatio, 0.82f)) * Projectile.scale;
            float width = Projectile.width + MathF.Sin(MathHelper.Pi * completionRatio * 3f - Time / 5f) * 16f;
            return width * scale;
        }

        public Color TornadoColorFunction(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(0.95f, 0.8f, completionRatio, true) * Utils.GetLerpValue(0f, 0.12f, completionRatio, true) * Projectile.Opacity;
            return Color.Lerp(Color.Orange, Color.Yellow, 0.4f) * opacity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Top, Projectile.Bottom - Vector2.UnitY * 125f, Projectile.width * 0.72f, ref _);
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // Initialize the tornado drawer.
            TornadoDrawer ??= new(TornadoWidthFunction, TornadoColorFunction, null, true, InfernumEffectsRegistry.YharonInfernadoShader);

            // Calculate draw points for the tornado.
            Vector2[] drawPositions = new Vector2[8];
            for (int i = 0; i < drawPositions.Length; i++)
                drawPositions[i] = Vector2.Lerp(Projectile.Bottom, Projectile.Top, i / (float)(drawPositions.Length - 1f));

            // Prepare the shader.
            InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["edgeTaperPower"].SetValue(0.51f);
            InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["scrollSpeed"].SetValue(0.9f);
            InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["additiveNoiseStrength"].SetValue(2.15f);
            InfernumEffectsRegistry.YharonInfernadoShader.Shader.Parameters["subtractiveNoiseStrength"].SetValue(1.11f);
            InfernumEffectsRegistry.YharonInfernadoShader.SetShaderTexture(InfernumTextureRegistry.WavyNoise);
            InfernumEffectsRegistry.YharonInfernadoShader.SetShaderTexture2(InfernumTextureRegistry.SmokyNoise);

            // Draw the tornado.
            TornadoDrawer.DrawPixelated(drawPositions, -Main.screenPosition, 54);
        }

        // Disable damage if the tornado is not suffiently faded in.
        public override bool? CanDamage() => Projectile.Opacity >= 0.8f ? null : false;
    }
}
