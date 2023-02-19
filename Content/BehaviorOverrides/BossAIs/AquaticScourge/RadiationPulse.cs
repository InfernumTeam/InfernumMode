using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class RadiationPulse : BaseMassiveExplosionProjectile
    {
        public override int Lifetime => 75;

        public override bool UsesScreenshake => false;

        public override Color GetCurrentExplosionColor(float pulseCompletionRatio) => Color.Lerp(Color.YellowGreen * 1.2f, Color.MediumPurple, MathHelper.Clamp(pulseCompletionRatio * 1.8f, 0f, 1f));

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public static float AcidWaterAccelerationFactor => 8f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Radiation Pulse");
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
        }

        public override void PostAI()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                if (CalamityUtils.CircularHitboxCollision(Projectile.Center, CurrentRadius * Projectile.scale * 0.325f, p.Hitbox))
                {
                    float increment = AcidWaterAccelerationFactor / CalamityPlayer.SulphSeaWaterSafetyTime;
                    if (p.Calamity().sulphurskin)
                        increment *= 0.5f;
                    if (p.Calamity().sulfurSet)
                        increment *= 0.5f;
                    p.Calamity().SulphWaterPoisoningLevel += increment;
                }
            }
        }

        public override bool? CanDamage() => false;
    }
}
