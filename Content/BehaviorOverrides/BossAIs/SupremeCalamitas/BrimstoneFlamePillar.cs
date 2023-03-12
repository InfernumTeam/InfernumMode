using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneFlamePillar : ModProjectile, IPixelPrimitiveDrawer
    {
        public int OwnerIndex;

        public PrimitiveTrailCopy FireDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float InitialRotationalOffset => ref Projectile.localAI[0];

        public const int Lifetime = 105;

        public float Height => MathHelper.Lerp(4f, Projectile.height, Projectile.scale * Projectile.Opacity);

        public float Width => MathHelper.Lerp(3f, Projectile.width, Projectile.scale * Projectile.Opacity);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Flame Pillar");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 2400;
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
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.04f, 0f, 1f);

            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime) * 2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item74, Projectile.Bottom);
                Projectile.position.Y += Projectile.height * 0.5f + 40f;
                Projectile.position.X -= Projectile.width * 0.5f;
                Projectile.netUpdate = true;
            }

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Bottom - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height * 0.5f;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Width * 0.5f, ref _);
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.9f;

        public float WidthFunction(float completionRatio)
        {
            float tipFadeoffInterpolant = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(1f, 0.75f, completionRatio, true));
            float baseFadeoffInterpolant = MathHelper.SmoothStep(2.4f, 1f, 1f - CalamityUtils.Convert01To010(Utils.GetLerpValue(0f, 0.19f, completionRatio, true)));
            float widthAdditionFactor = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -13f + Projectile.identity + completionRatio * MathHelper.Pi * 4f) * 0.2f;
            return Width * tipFadeoffInterpolant * baseFadeoffInterpolant * (1f + widthAdditionFactor);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color darkFlameColor = new(249, 59, 91);
            Color lightFlameColor = new(174, 45, 237);
            float colorShiftInterpolant = (float)Math.Sin(-Main.GlobalTimeWrappedHourly * 2.7f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f;
            Color color = Color.Lerp(darkFlameColor, lightFlameColor, (float)Math.Pow(colorShiftInterpolant, 1.64f));
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DarkFlamePillarVertexShader);

            // Create a telegraph line upward that fades away away the pillar fades in.
            Vector2 start = Projectile.Top;
            Vector2 end = start - Vector2.UnitY.RotatedBy(Projectile.rotation) * Height;
            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(1.4f);
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakFaded.Value;

            List<Vector2> points = new();
            for (int i = 0; i <= 64; i++)
                points.Add(Vector2.Lerp(start, end, i / 64f) + Vector2.UnitY * 20f);

            if (Time >= 2f)
                FireDrawer.DrawPixelated(points, Projectile.Size * new Vector2(0f, 0.5f) - Main.screenPosition, 166);
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
