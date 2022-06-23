using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class EoCTooth2 : ModProjectile
    {
        public Player Target => Main.player[(int)Projectile.ai[0]];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Projectile.velocity.Y < 8f)
                Projectile.velocity.Y += 0.26f;
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 72, 0, 255);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }
        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
