using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Generic
{
    public class BrimstoneForcefieldExplosion : BaseMassiveExplosionProjectile
    {
        public override int Lifetime => 32;
        public override bool UsesScreenshake => false;
        public override Color GetCurrentExplosionColor(float pulseCompletionRatio) => Color.Lerp(Color.Red, Color.Orange, Clamp(pulseCompletionRatio * 1.54f, 0f, 1f)) * 6f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Generic;
        }

        public override void PostAI() => Lighting.AddLight(Projectile.Center, 0.2f, 0f, 0f);
    }
}
