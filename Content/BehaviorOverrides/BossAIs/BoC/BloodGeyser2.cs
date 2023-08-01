using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BoC
{
    public class BloodGeyser2 : ModProjectile
    {
        internal const float Gravity = 0.25f;
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor Geyser");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 420;
            Projectile.penetrate = 1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.tileCollide = Projectile.timeLeft < 220;
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Release blood idly.
            Dust blood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default, 0.5f);
            blood.velocity = Vector2.Zero;
            blood.noGravity = true;

            Projectile.velocity.Y += Gravity;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(246, 195, 80, Projectile.alpha);
    }
}
