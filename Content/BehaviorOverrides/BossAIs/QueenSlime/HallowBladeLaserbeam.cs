using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class HallowBladeLaserbeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        } = null;

        public Vector2 LaserStart => Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;

        public Vector2 LaserEnd => Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;

        public static int Lifetime => 300;

        public static float MaxLaserLength => 5000f;

        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLength => ref Projectile.ai[1];

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Hallow Blade Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 270;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            float lifetimeCompletion = Utils.GetLerpValue(0f, Lifetime, Time, true);
            Projectile.scale = CalamityUtils.Convert01To010(lifetimeCompletion) * 4.2f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Make the laser expand outward.
            LaserLength = MathHelper.Clamp(LaserLength + 180f, 100f, MaxLaserLength);

            Time++;
        }

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width * 2f;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = 0.5f * MathF.Sin(-9f * Main.GlobalTimeWrappedHourly) + 0.5f;
            return Color.Lerp(Color.HotPink, Color.Cyan, 0.25f * colorInterpolant);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), LaserStart, LaserEnd, Projectile.width * Projectile.scale, ref _);
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);
            Vector2[] baseDrawPoints = new Vector2[20];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(LaserStart, LaserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.85f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(LaserColorFunction(0f));
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.SmokyNoise);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");
            InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue(1f / 2.7f);

            LaserDrawer.DrawPixelated(baseDrawPoints, -Main.screenPosition, 20);
        }
    }
}
