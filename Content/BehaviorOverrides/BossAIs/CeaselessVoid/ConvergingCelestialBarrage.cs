using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class ConvergingCelestialBarrage : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float IdealDirection => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Otherwordly Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
        }

        public static Vector2 DetermineVelocity(Vector2 old, float idealDirection)
        {
            return (Vector2.Lerp(old, idealDirection.ToRotationVector2() * old.Length(), 0.0132f) * 1.03f).ClampMagnitude(0f, 30f);
        }

        public static Vector2 SimulateMotion(Vector2 startingPosition, Vector2 startingVelocity, float idealDirection, int frames)
        {
            Vector2 endingPosition = startingPosition;
            Vector2 velocity = startingVelocity;
            for (int i = 0; i < frames; i++)
            {
                endingPosition += velocity;
                velocity = DetermineVelocity(velocity, idealDirection);
            }
            return endingPosition;
        }

        public override void AI()
        {
            Projectile.velocity = DetermineVelocity(Projectile.velocity, IdealDirection);
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.1f;

        public override Color? GetAlpha(Color lightColor) => new Color(255, 150, 255, 108) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            return false;
        }
    }
}
