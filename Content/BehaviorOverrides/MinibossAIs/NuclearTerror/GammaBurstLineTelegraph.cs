using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.NuclearTerror
{
    public class GammaBurstLineTelegraph : ModProjectile
    {
        public PrimitiveTrailCopy TelegraphDrawer
        {
            get;
            set;
        }

        public static int Lifetime => 54;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Line Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 124;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = LumUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.Opacity = Projectile.scale;
        }

        public override bool ShouldUpdatePosition() => true;

        public float TelegraphWidthFunction(float completionRatio) => Projectile.scale * Projectile.width;

        public Color TelegraphColorFunction(float completionRatio)
        {
            float endFadeOpacity = Utils.GetLerpValue(0f, 0.15f, completionRatio, true) * Utils.GetLerpValue(1f, 0.8f, completionRatio, true);
            return Color.Lime * endFadeOpacity * Projectile.Opacity * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Initialize the telegraph drawer.
            TelegraphDrawer ??= new(TelegraphWidthFunction, TelegraphColorFunction, null, false, InfernumEffectsRegistry.SideStreakVertexShader);

            // Draw the telegraph line.
            Vector2 telegraphDirection = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 telegraphStart = Projectile.Center;
            Vector2 telegraphEnd = Projectile.Center + telegraphDirection * 5000f;
            Vector2[] telegraphPoints =
            [
                telegraphStart,
                (telegraphStart + telegraphEnd) * 0.5f,
                telegraphEnd
            ];
            TelegraphDrawer.Draw(telegraphPoints, -Main.screenPosition, 44);
            return false;
        }
    }
}
