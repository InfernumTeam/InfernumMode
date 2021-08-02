using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EyeOfCthulhu
{
    public class EoCTooth2 : ModProjectile
    {
        public Player Target => Main.player[(int)projectile.ai[0]];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = false;
			projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.velocity.Y < 14f)
                projectile.velocity.Y += 0.26f;
            projectile.alpha = Utils.Clamp(projectile.alpha - 72, 0, 255);

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }
        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
