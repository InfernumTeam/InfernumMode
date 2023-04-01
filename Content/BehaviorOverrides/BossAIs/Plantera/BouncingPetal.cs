using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Plantera
{
    public class BouncingPetal : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SeedPlantera}";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Petal");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 480;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Bounce after hitting a tile.
            Projectile.velocity = -oldVelocity * 0.35f;
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
