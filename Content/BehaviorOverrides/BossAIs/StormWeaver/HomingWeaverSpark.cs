using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver
{
    public class HomingWeaverSpark : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy ElectricTrailDrawer
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spark");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Home in on the target at first.
            if (Time < 35f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = (Projectile.velocity * 19f + Projectile.SafeDirectionTo(target.Center) * 21f) / 20f;
            }

            // Accelerate after homing.
            else if (Projectile.velocity.Length() < 36f)
                Projectile.velocity *= 1.026f;

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 56) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, lightColor, 4f);
            return false;
        }

        public float TrailWidthFunction(float completionRatio)
        {
            return SmoothStep(25f, 2f, completionRatio) * Projectile.Opacity;
        }

        public Color TrailColorFunction(float completionRatio)
        {
            return Color.Cyan * Sqrt(completionRatio) * Projectile.Opacity;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // Initialize the trail drawer.
            ElectricTrailDrawer ??= new(TrailWidthFunction, TrailColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakLightning);
            ElectricTrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 61);
        }
    }
}
