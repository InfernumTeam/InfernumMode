using CalamityMod;
using CalamityMod.Events;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class IchorSpit : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public bool CanHomeIn => Projectile.ai[1] == 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor");

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

            if (CanHomeIn)
            {
                // Projectiles of this kind that can home in should have a lower life time.
                if (Projectile.timeLeft > 220)
                    Projectile.timeLeft = 220;

                // If this projectile is not close to death, home in.
                if (Time > 55f)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    float flySpeed = 10f;
                    if (BossRushEvent.BossRushActive)
                        flySpeed *= 2f;
                    if (!Projectile.WithinRange(target.Center, 50f))
                        Projectile.velocity = (Projectile.velocity * 44f + Projectile.SafeDirectionTo(target.Center) * flySpeed) / 45f;
                }
            }

            // Release ichor dust idly.
            Dust ichor = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 170, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 100, default, 1.5f);
            ichor.velocity = Projectile.velocity;
            ichor.scale *= 0.6f;
            ichor.noGravity = true;

            Projectile.rotation += Projectile.direction * 0.3f;
            Time++;
        }

        

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Ichor, 45);

        public override void Kill(int timeLeft)
        {
            // Make a sound and release some ichor dust on death.
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            for (int i = 0; i < 6; i++)
            {
                Dust ichor = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 170, -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 2.5f);
                ichor.noGravity = true;
                ichor.velocity *= 2f;

                ichor = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 170, -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
                ichor.velocity *= 2f;
            }
        }
    }
}
