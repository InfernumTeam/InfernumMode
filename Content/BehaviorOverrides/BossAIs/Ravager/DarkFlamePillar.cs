using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class DarkFlamePillar : ModProjectile, IPixelPrimitiveDrawer
    {
        public int OwnerIndex;

        public PrimitiveTrailCopy FireDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float InitialRotationalOffset => ref Projectile.localAI[0];

        public const int Lifetime = 136;

        public float Height => Lerp(4f, Projectile.height, Projectile.scale * Projectile.Opacity);

        public float Width => Lerp(3f, Projectile.width, Projectile.scale * Projectile.Opacity);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Dark Flame Pillar");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 960;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.MaxUpdates = 2;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.04f, 0f, 1f);

            Projectile.scale = Sin(Pi * Time / Lifetime) * 2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Top;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height * 0.72f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Width * 0.82f, ref _);
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.9f;

        public float WidthFunction(float completionRatio)
        {
            float tipFadeoffInterpolant = SmoothStep(0f, 1f, Utils.GetLerpValue(1f, 0.75f, completionRatio, true));
            float baseFadeoffInterpolant = SmoothStep(2.4f, 1f, 1f - CalamityUtils.Convert01To010(Utils.GetLerpValue(0f, 0.19f, completionRatio, true)));
            float widthAdditionFactor = Sin(Main.GlobalTimeWrappedHourly * -13f + Projectile.identity + completionRatio * Pi * 4f) * 0.2f;
            return Width * tipFadeoffInterpolant * baseFadeoffInterpolant * (1f + widthAdditionFactor);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color darkFlameColor = new(58, 107, 252);
            Color lightFlameColor = new(45, 207, 239);
            float colorShiftInterpolant = Sin(-Main.GlobalTimeWrappedHourly * 6.7f + completionRatio * TwoPi) * 0.5f + 0.5f;
            Color color = Color.Lerp(darkFlameColor, lightFlameColor, Pow(colorShiftInterpolant, 1.64f));
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Create a telegraph line upward that fades away away the pillar fades in.
            Vector2 start = Projectile.Top;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height;
            if (Projectile.Opacity < 1f)
                Main.spriteBatch.DrawLineBetter(start + Projectile.Size * 0.5f, end + Projectile.Size * 0.5f, Color.Cyan * (1f - Projectile.Opacity), Projectile.Opacity * 6f);

            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DarkFlamePillarVertexShader);

            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(1.4f);
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakFaded.Value;

            Vector2 start = Projectile.Top;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height;

            List<Vector2> points = new();
            for (int i = 0; i <= 64; i++)
                points.Add(Vector2.Lerp(start, end, i / 64f) + Vector2.UnitY * 20f);

            if (Time >= 2f)
                FireDrawer.DrawPixelated(points, Projectile.Size * 0.5f - Main.screenPosition, 166);
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
