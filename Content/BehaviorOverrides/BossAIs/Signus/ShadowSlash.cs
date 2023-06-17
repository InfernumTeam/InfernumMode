using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class ShadowSlash : ModProjectile
    {
        public PrimitiveTrailCopy SlashDrawer
        {
            get;
            set;
        }

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public static int Lifetime => 20;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Slash");
        }

        public override void SetDefaults()
        {
            Projectile.width = 640;
            Projectile.height = 100;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Projectile.timeLeft / (float)Lifetime;
            Projectile.scale = Utils.GetLerpValue(Lifetime, Lifetime - 5f, Projectile.timeLeft, true);
            Projectile.scale *= Lerp(0.7f, 1.1f, Projectile.identity % 6f / 6f) * 0.5f;
            Projectile.rotation = Projectile.ai[0];
        }

        public override Color? GetAlpha(Color lightColor) => Color.DarkViolet * Projectile.Opacity * 1.4f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Center - Projectile.rotation.ToRotationVector2() * Projectile.width * Projectile.scale * 0.5f;
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.width * Projectile.scale * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.height * 0.5f, ref _);
        }

        public static float PrimitiveWidthFunction(float completionRatio) => Utils.GetLerpValue(0f, 0.32f, completionRatio, true) * Utils.GetLerpValue(1f, 0.68f, completionRatio, true) * 40f;

        public Color PrimitiveColorFunction2(float completionRatio) => Color.Lerp(Color.Cyan, Color.Fuchsia, Pow(Projectile.Opacity, 0.5f)) with { A = 0 };

        public override bool PreDraw(ref Color lightColor)
        {
            // Initialize the slash drawer.
            var slashShader = InfernumEffectsRegistry.DoGDashIndicatorVertexShader;
            SlashDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction2, null, true, slashShader);

            // Calculate the three points that define the overall shape of the slash.
            Vector2 start = Projectile.Center - Projectile.rotation.ToRotationVector2() * Projectile.width * Projectile.scale * 0.5f;
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.width * Projectile.scale * 0.5f;
            Vector2 middle = (start + end) * 0.5f + (Projectile.rotation + PiOver2).ToRotationVector2() * Projectile.width * Projectile.scale * 0.167f;

            // Create a bunch of points that slash across the Bezier curve created from the above three points.
            List<Vector2> slashPoints = new();
            for (int i = 0; i < 16; i++)
            {
                float interpolant = i / 15f * Pow(1f - Projectile.Opacity, 0.4f);
                slashPoints.Add(Utilities.QuadraticBezier(start, middle, end, interpolant));
            }

            slashShader.UseOpacity(Pow(Projectile.Opacity, 0.35f));
            slashShader.UseImage1("Images/Extra_194");
            slashShader.UseColor(PrimitiveColorFunction2(0.5f));
            SlashDrawer.Draw(slashPoints, -Main.screenPosition, 50);
            SlashDrawer.Draw(slashPoints, -Main.screenPosition, 24);

            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            ScreenOverlaysSystem.DrawCacheProjsOverSignusBlackening.Add(index);
        }
    }
}
