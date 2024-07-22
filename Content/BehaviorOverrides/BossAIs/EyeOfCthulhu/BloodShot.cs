using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class BloodShot : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public bool CanHomeIn => Projectile.ai[1] == 1f;
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Blood");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.light = 0.6f;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            
        }

        public override void AI()
        {
            // Make a blood-like sound on the first frame of this projectile's existence.
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item17, Projectile.position);
                Projectile.localAI[0] = 1f;
            }

            // Projectiles of this kind that can home in should have a lower life time.
            if (Projectile.timeLeft > 220)
                Projectile.timeLeft = 220;

            // If this projectile is not close to death, home in.
            if (Time > 55f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                float flySpeed = Projectile.Distance(target.Center) * 0.012f + 8.4f;
                if (BossRushEvent.BossRushActive)
                    flySpeed *= 2.15f;
                if (!Projectile.WithinRange(target.Center, 50f))
                    Projectile.velocity = (Projectile.velocity * 44f + Projectile.SafeDirectionTo(target.Center) * flySpeed) / 45f;
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, 0.5f, 0f, 1f);

            // Release blood dust idly.
            Dust blood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 100, default, 1.5f);
            blood.velocity = Projectile.velocity;
            blood.noGravity = true;

            Projectile.rotation += Projectile.direction * 0.3f;
            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.75f) * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            // Make a sound and release some blood dust on death.
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            for (int i = 0; i < 6; i++)
            {
                Dust blood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 2.5f);
                blood.noGravity = true;
                blood.velocity *= 2f;

                blood = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
                blood.velocity *= 2f;
            }
        }
    }
}
