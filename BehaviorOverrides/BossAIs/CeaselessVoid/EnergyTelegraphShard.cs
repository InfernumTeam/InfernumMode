using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class EnergyTelegraphShard : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy Shard");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 27;
            Projectile.scale = 2f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(27f, 22f, Projectile.timeLeft) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.Cyan, Color.Fuchsia, Projectile.ai[0]) * Projectile.Opacity;
    }
}
