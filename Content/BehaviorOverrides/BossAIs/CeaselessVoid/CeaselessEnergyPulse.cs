using CalamityMod;
using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessEnergyPulse : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 180;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxRadius => 1100f;

        public override float MinScale => 1f;

        public override float MaxScale => 1.25f;

        public override float RadiusExpandRateInterpolant => 0.1f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ceaseless Energy Pulse");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) =>
            Utils.Remap(distanceFromPlayer, 600f, 1500f, 8f, 0f);

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio) =>
            Color.Lerp(Color.MediumPurple, Color.DarkBlue, MathHelper.Clamp(lifetimeCompletionRatio * 5f, 0f, 0.75f));

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft <= 42f)
                Projectile.damage = 0;

            for (int i = 0; i < 2; i++)
                base.PreDraw(ref lightColor);
            return false;
        }
    }
}
