using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Destroyer
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
            CooldownSlot = ImmunityCooldownID.Bosses;
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
            // Create particles and sounds at the explosion point.
            for (int i = 0; i < 20; i++)
            {
                Color fireColor = Main.rand.NextBool() ? Color.Yellow : Color.Red;
                CloudParticle fireCloud = new(Projectile.Center, Main.rand.NextVector2Circular(8f, 8f), fireColor, Color.DarkGray, 54, Main.rand.NextFloat(2f, 3f));
                GeneralParticleHandler.SpawnParticle(fireCloud);
            }
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
