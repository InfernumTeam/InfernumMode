using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessEnergy : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ceaseless Energy");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Time, true) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Accelerate.
            if (Projectile.velocity.Length() < 23f)
                Projectile.velocity *= BossRushEvent.BossRushActive ? 1.03f : 1.023f;

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float alpha = Utils.GetLerpValue(0f, 30f, Time, true);
            return new Color(1f, 1f, 1f, alpha) * Projectile.Opacity * MathHelper.Lerp(0.6f, 1f, alpha);
        }

        public override bool? CanDamage() => Projectile.Opacity >= 1f ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.A = 0;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
