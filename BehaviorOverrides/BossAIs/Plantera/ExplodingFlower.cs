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
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(150f, 130f, Projectile.timeLeft, true);
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            for (int i = 0; i < 3; i++)
            {
                float offsetAngle = MathHelper.Lerp(-0.38f, 0.38f, i / 2f);
                Vector2 petalShootVelocity = Projectile.SafeDirectionTo(closestPlayer.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * 6.5f;
                Utilities.NewProjectileBetter(Projectile.Center, petalShootVelocity, ModContent.ProjectileType<Petal>(), 150, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
    }
}
