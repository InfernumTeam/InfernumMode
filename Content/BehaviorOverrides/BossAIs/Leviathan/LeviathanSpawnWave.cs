using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class LeviathanSpawnWave : ModProjectile, IPixelPrimitiveDrawer
    {
        internal PrimitiveTrailCopy TornadoDrawer;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float Time => ref Projectile.ai[0];
        public ref float WaveHeight => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wave");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 150;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 1020;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 360;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            if (WaveHeight < 60f)
                WaveHeight = 60f;
            WaveHeight = Lerp(WaveHeight, 300f, 0.04f);
            Projectile.Opacity = Sin(Projectile.timeLeft / 360f * Pi) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float opacity = Sin(completionRatio * Pi) * Projectile.Opacity;
            return Color.Lerp(Color.DeepSkyBlue, Color.Blue, Math.Abs(Sin(completionRatio * Pi + Main.GlobalTimeWrappedHourly)) * 0.5f) * opacity;
        }

        internal float WidthFunction(float completionRatio) => WaveHeight * Sin(completionRatio * Pi);

        internal Vector2 OffsetFunction(float completionRatio) => Vector2.UnitY * Sin(completionRatio * Pi * 3f + Time / 13f) * 30f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float _ = 0f;
                float completionRatio = i / (float)Projectile.oldPos.Length;
                Vector2 top = Projectile.oldPos[i] + OffsetFunction(completionRatio);
                Vector2 bottom = Projectile.oldPos[i] + Vector2.UnitY * WidthFunction(completionRatio) + OffsetFunction(completionRatio);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, (int)Projectile.velocity.X, ref _))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            TornadoDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.DukeTornadoVertexShader);

            InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            for (int i = 0; i < 3; i++)
                TornadoDrawer.DrawPixelated(Projectile.oldPos, Vector2.UnitY * WaveHeight * 0.5f - Main.screenPosition, 35, 0f);
        }
    }
}
