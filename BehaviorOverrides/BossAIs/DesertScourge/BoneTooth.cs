using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DesertScourge
{
    public class BoneTooth : ModProjectile
    {
        public Player Target => Main.player[(int)projectile.ai[0]];
        public bool HasTouchedGroundYet
		{
            get => projectile.ai[1] == 1f;
            set => projectile.ai[1] = value.ToInt();
		}
        public override void SetStaticDefaults() => DisplayName.SetDefault("Tooth");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = true;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (projectile.velocity.Y < 16f)
                projectile.velocity.Y += 0.3f;
            projectile.alpha = Utils.Clamp(projectile.alpha - 72, 0, 255);

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // Only collide if close vertically to the target and not already stuck in the ground.
            if (!HasTouchedGroundYet)
                projectile.tileCollide = projectile.Bottom.Y > Target.Top.Y + 30;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!HasTouchedGroundYet)
			{
                HasTouchedGroundYet = true;
                projectile.velocity.X = 0f;
                projectile.netUpdate = true;
            }
            return false;
        }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
            fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
