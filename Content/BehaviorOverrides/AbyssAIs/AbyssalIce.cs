using CalamityMod.Events;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class AbyssalIce : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Abyssal Ice");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            // Decide rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 32f, Projectile.timeLeft, true);

            // Fall and horizontally decelerate.
            Projectile.velocity.X *= 0.97f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.2f, -8f, 15f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0));
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
