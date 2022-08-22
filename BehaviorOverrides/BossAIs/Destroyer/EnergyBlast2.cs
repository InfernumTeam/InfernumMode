using CalamityMod;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class EnergyBlast2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy Blast");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 76;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.velocity = (Projectile.velocity * 59f + Projectile.SafeDirectionTo(closestPlayer.Center) * 9f) / 60f;

            if (Projectile.WithinRange(closestPlayer.Center, 300f) || Projectile.Opacity < 1f)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.05f, 0f, 1f);

            if (Projectile.Opacity <= 0f)
                Projectile.Kill();

            Lighting.AddLight(Projectile.Center, Vector3.One);
        }

        // Explode on death.
        public override void Kill(int timeLeft)
        {
            Utilities.CreateGenericDustExplosion(Projectile.Center, 235, 35, 12f, 4.25f);
            SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 35; i++)
            {
                Vector2 fireVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(14f, 21f);
                Utilities.NewProjectileBetter(Projectile.Center, fireVelocity, ModContent.ProjectileType<EnergySpark>(), 125, 0f);
            }
        }

        
    }
}
