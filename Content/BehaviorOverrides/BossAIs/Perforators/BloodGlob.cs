using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class BloodGlob : ModProjectile
    {
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Blood Glob");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.penetrate = -1;
            
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y - 0.25f, -20f, 20f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
