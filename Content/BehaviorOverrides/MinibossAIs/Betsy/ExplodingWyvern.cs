using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Betsy
{
    public class ExplodingWyvern : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wyvern");
            Main.projFrames[Projectile.type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.9f;
            Projectile.Opacity = Utils.GetLerpValue(210f, 198f, Projectile.timeLeft, true);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 6 == 5)
                Projectile.frame++;

            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float offsetAngle = Main.rand.NextBool() ? PiOver4 : 0f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 shootVelocity = (TwoPi * i / 4f + offsetAngle).ToRotationVector2() * 11f;
                Utilities.NewProjectileBetter(Projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<DraconicBurst>(), 170, 0f);
            }
        }
    }
}
