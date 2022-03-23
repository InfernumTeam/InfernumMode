using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Betsy
{
    public class ExplodingWyvern : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wyvern");
            Main.projFrames[projectile.type] = 10;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            projectile.velocity *= 0.9f;
            projectile.Opacity = Utils.InverseLerp(210f, 198f, projectile.timeLeft, true);

            projectile.frameCounter++;
            if (projectile.frameCounter % 6 == 5)
                projectile.frame++;

            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float offsetAngle = Main.rand.NextBool() ? MathHelper.PiOver4 : 0f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 4f + offsetAngle).ToRotationVector2() * 11f;
                Utilities.NewProjectileBetter(projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<DraconicBurst>(), 170, 0f);
            }
        }
    }
}
