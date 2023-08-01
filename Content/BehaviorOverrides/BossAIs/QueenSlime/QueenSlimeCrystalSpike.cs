using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeCrystalSpike : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Spike");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Accelerate over time.
            if (Projectile.velocity.Length() < 25f)
                Projectile.velocity *= 1.02f;

            // Interact with tiles after a short amount of time has passed.
            Projectile.tileCollide = Time >= 12f;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Create a crystal shatter sound.
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            // Create a bunch of crystal shards.
            for (int i = 0; i < 8; i++)
            {
                Dust crystalShard = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.Next(DustID.BlueCrystalShard, DustID.PurpleCrystalShard + 1));
                crystalShard.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2.5f;
                crystalShard.noGravity = Main.rand.NextBool();
                crystalShard.scale = Main.rand.NextFloat(0.9f, 1.3f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 } * 0.65f, Color.White, 4f);
            return false;
        }
    }
}