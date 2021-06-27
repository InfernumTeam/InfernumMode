using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Cryogen
{
    public class IceBomb2 : ModProjectile
    {
        public ref float Time => ref projectile.localAI[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Bomb");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 34;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 180;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 20f, Time, true) * Utils.InverseLerp(0f, 20f, projectile.timeLeft, true);
            projectile.scale = MathHelper.Lerp(1f, 1.7f, 1f - Utils.InverseLerp(0f, 20f, projectile.timeLeft, true));
            projectile.velocity *= 0.98f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 spikeVelocity = -Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.33f, 0.33f, i / 2f)) * 5f;
                Utilities.NewProjectileBetter(projectile.Center, spikeVelocity, ModContent.ProjectileType<IceRain2>(), 120, 0f);
            }
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
