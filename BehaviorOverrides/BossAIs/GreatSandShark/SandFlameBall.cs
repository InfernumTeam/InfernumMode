using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class SandFlameBall : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Ball");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 100;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Play a wind sound.
            if (projectile.localAI[0] == 0f)
			{
                Main.PlaySound(SoundID.DD2_BookStaffCast, projectile.Center);
                projectile.localAI[0] = 1f;
			}

            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            // Determine frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            Time++;
		}

		public override void Kill(int timeLeft)
		{
            Utilities.CreateGenericDustExplosion(projectile.Center, 32, 15, 8f, 1.2f);
            Utilities.CreateGenericDustExplosion(projectile.Center, 65, 8, 9f, 1.35f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into desert flames.
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            for (int i = 0; i < 3; i++)
            {
                Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.65f, 0.65f, i / 2f)) * 8f;
                int fuck = Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ProjectileID.DesertDjinnCurse, 160, 0f);
                Main.projectile[fuck].ai[0] = target.whoAmI;
            }
		}

		public override bool CanDamage() => projectile.Opacity > 0.9f;
	}
}
