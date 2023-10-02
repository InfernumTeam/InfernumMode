using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.KingSlime
{
    public class Shuriken : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Shuriken}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Shuriken");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * 0.4f;
            Projectile.tileCollide = Projectile.timeLeft < 90;

            if (Projectile.velocity.Length() < 9.5f)
                Projectile.velocity *= 1.0145f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            Utilities.DrawProjectileWithBackglowTemp(Projectile, Color.White with { A = 0 }, lightColor, 3f);
            return false;
        }

        public override void OnKill(int timeLeft) => Collision.HitTiles(Projectile.position, Projectile.velocity, 24, 24);
    }
}
