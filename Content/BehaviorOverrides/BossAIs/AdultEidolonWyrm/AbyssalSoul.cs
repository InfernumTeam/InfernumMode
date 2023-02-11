using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AbyssalSoul : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float AngularVelocity => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Abyssal Spirit");
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];
            Projectile.velocity = PerformMovementStep(Projectile.velocity, AngularVelocity);

            Time++;
        }

        public static Vector2 PerformMovementStep(Vector2 oldVelocity, float angularVelocity)
        {
            // Accelerate and arc over time.
            if (oldVelocity.Length() >= 35f)
                return oldVelocity;

            return oldVelocity.RotatedBy(angularVelocity) * 1.02f;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color soulColor = Color.Lerp(Color.White, Color.Red, Projectile.identity % 8f / 11f);
            soulColor.A = 0;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, frame, soulColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0));

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 5f;
                ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition + drawOffset, frame, soulColor * Projectile.Opacity * 0.4f, Projectile.rotation, origin, Projectile.scale, 0, 0));

                Vector2 afterimageDrawPosition = Vector2.Lerp(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Center, 0.4f) - Main.screenPosition;
                float afterimageColor = 1f - i / 4f;
                ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, afterimageDrawPosition, frame, soulColor * Projectile.Opacity * afterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0));
            }
            
            return false;
        }
    }
}
