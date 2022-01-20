using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkMagicBomb : ModProjectile
    {
        public ref float TimeCountdown => ref projectile.ai[0];
        public ref float SpiritHue => ref projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Bomb");
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 44;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 40;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.DD2_ExplosiveTrapExplode, projectile.Center);
            Utilities.CreateGenericDustExplosion(projectile.Center, 242, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 12; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 16f;
                Utilities.NewProjectileBetter(projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 540, 0f);
            }
        }
    }
}
