using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class FalllingCrystal : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/Extra_{ExtrasID.QueenSlimeCrystalCore}";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hallow Crystal");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 480;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.ai[0] == 1f)
            {
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.4f * Math.Sign(Projectile.velocity.Y), -19f, 19f);
                Projectile.tileCollide = true;
            }

            // Jitter in place slightly if not accelerating.
            else
                Projectile.Center += Main.rand.NextVector2Circular(0.65f, 0.65f);

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Create a crystal shatter sound.
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            // Create a bunch of crystal shards.
            for (int i = 0; i < 15; i++)
            {
                Dust crystalShard = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.Next(DustID.BlueCrystalShard, DustID.PurpleCrystalShard + 1));
                crystalShard.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2.5f;
                crystalShard.noGravity = Main.rand.NextBool();
                crystalShard.scale = Main.rand.NextFloat(0.9f, 1.3f);
            }

            for (int i = 1; i <= 3; i++)
                Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2CircularEdge(4f, 4f), Mod.Find<ModGore>($"QSCrystal{i}").Type, Projectile.scale);
        }

        public override bool? CanDamage() => Time >= 27f;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.HotPink with { A = 0 }, Color.White, Utils.GetLerpValue(0f, 35f, Time, true)) * Projectile.Opacity;
    }
}
