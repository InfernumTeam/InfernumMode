using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyFireWall : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FlameDrawer { get; private set; } = null;

        public const int Lifetime = 650;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Wall");
        }

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 1000;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = false;
            Projectile.Opacity = 0;
            Projectile.scale = 0;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Rapidly fade in.
            if (Projectile.timeLeft >= Lifetime - 100)
            {
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.025f, 0f, 1f);
                Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.025f, 0f, 1f);
            }

            // Fade out.
            if (Projectile.timeLeft <= 40)
            {
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.025f, 0f, 1f);
                Projectile.scale = MathHelper.Clamp(Projectile.scale - 0.025f, 0f, 1f);
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.75f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = 120f * Projectile.scale;
            Vector2 topStart = Projectile.Center - new Vector2(0, 130);
            Vector2 topEnd = topStart - Vector2.UnitY * (2000 - 80f);
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), topStart, topEnd, width, ref _))
                return true;

            Vector2 bottomStart = Projectile.Center + new Vector2(0, 130);
            Vector2 bottomEnd = bottomStart + Vector2.UnitY * (2000 - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), bottomStart, bottomEnd, width, ref _);
        }

        public float WidthFunction(float completionRatio) => 150f * Projectile.scale;

        public Color ColorFunction(float completionRatio) => new(255, 191, 73);

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            FlameDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVertexShader);

            // The gap is determined by the projectile center, and thus controlled by the attacking guardian.
            // Draw a set distance above and below the center to give a gap in the wall.
            float laserDistance = 2000f;
            Vector2 topBaseDrawPos = Projectile.Center + new Vector2(0f, -75f);
            Vector2[] topDrawPoints = new Vector2[8];
            for (int i = 0; i < topDrawPoints.Length; i++)
                topDrawPoints[i] = Vector2.Lerp(topBaseDrawPos, new Vector2(topBaseDrawPos.X, topBaseDrawPos.Y - laserDistance), (float)i / topDrawPoints.Length);

            Vector2 bottomBaseDrawPos = Projectile.Center + new Vector2(0f, 75f);
            Vector2[] bottomDrawPoints = new Vector2[8];
            for (int i = 0; i < bottomDrawPoints.Length; i++)
                bottomDrawPoints[i] = Vector2.Lerp(bottomBaseDrawPos, new Vector2(bottomBaseDrawPos.X, bottomBaseDrawPos.Y + laserDistance), (float)i / bottomDrawPoints.Length);

            InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);
            InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(new Color(255, 255, 150));
            InfernumEffectsRegistry.GenericLaserVertexShader.Shader.Parameters["strongerFade"].SetValue(true);

            FlameDrawer.DrawPixelated(topDrawPoints, -Main.screenPosition, 20);

            FlameDrawer.DrawPixelated(bottomDrawPoints, -Main.screenPosition, 20);
        }
    }
}
