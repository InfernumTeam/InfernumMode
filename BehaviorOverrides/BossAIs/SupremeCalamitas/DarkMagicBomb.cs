using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkMagicBomb : ModProjectile
    {
        public ref float TimeCountdown => ref Projectile.ai[0];
        public ref float SpiritHue => ref Projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Bomb");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 40;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, 242, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 16f;
                Utilities.NewProjectileBetter(Projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<DarkMagicFlame>(), 500, 0f);
            }
        }
    }
}
