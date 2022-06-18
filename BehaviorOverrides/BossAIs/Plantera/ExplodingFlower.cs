using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class ExplodingFlower : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Flower");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 150;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.scale = Utils.InverseLerp(0f, 20f, projectile.timeLeft, true) * Utils.InverseLerp(150f, 130f, projectile.timeLeft, true);
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            for (int i = 0; i < 3; i++)
            {
                float offsetAngle = MathHelper.Lerp(-0.38f, 0.38f, i / 2f);
                Vector2 petalShootVelocity = projectile.SafeDirectionTo(closestPlayer.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * 6.5f;
                Utilities.NewProjectileBetter(projectile.Center, petalShootVelocity, ModContent.ProjectileType<Petal>(), 150, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
    }
}
