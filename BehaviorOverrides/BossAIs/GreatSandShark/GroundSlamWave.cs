using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class GroundSlamWave : ModProjectile
    {
        internal PrimitiveTrailCopy TornadoDrawer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float Time => ref Projectile.ai[0];

        public ref float WaveHeight => ref Projectile.ai[1];

        public const int Lifetime = 240;
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wave");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 96;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 60;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            if (WaveHeight < 6f)
                WaveHeight = 6f;
            WaveHeight = MathHelper.Lerp(WaveHeight, 64f, 0.08f);
            Projectile.Opacity = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            Time++;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, (float)Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)) * 0.5f);
            return c * Projectile.Opacity;
        }

        public float WidthFunction(float completionRatio) => WaveHeight;
        
        public Vector2 OffsetFunction(float completionRatio) => Vector2.UnitY * (float)Math.Sin(completionRatio * MathHelper.Pi + Time / 11f) * 16f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float _ = 0f;
                float completionRatio = i / (float)Projectile.oldPos.Length;
                Vector2 top = Projectile.oldPos[i] + OffsetFunction(completionRatio);
                Vector2 bottom = Projectile.oldPos[i] + Vector2.UnitY * WaveHeight + OffsetFunction(completionRatio);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, (int)Math.Abs(Projectile.velocity.X) * 2f, ref _))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            TornadoDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, OffsetFunction, false, InfernumEffectsRegistry.DukeTornadoVertexShader);

            InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            for (int i = 0; i < 3; i++)
                TornadoDrawer.Draw(Projectile.oldPos, Vector2.UnitY * WaveHeight * 0.5f - Main.screenPosition, 35, 0f);
            return false;
        }
    }
}
