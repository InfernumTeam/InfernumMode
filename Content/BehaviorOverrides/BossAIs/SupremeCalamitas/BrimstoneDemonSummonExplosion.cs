using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneDemonSummonExplosion : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brimstone Explosion");
            Main.projFrames[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5;
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.Kill();

            Time++;

            // Create brimstone fire dust.
            Vector2 fireSpawnPosition = Projectile.Bottom - Vector2.UnitY * 20f + Main.rand.NextVector2Circular(24f, 10f);
            Vector2 fireDustVelocity = (fireSpawnPosition - Projectile.Bottom).SafeNormalize(-Vector2.UnitY).RotatedByRandom(0.19f);
            fireDustVelocity *= Main.rand.NextFloat(2f, 7f);

            Dust brimstoneFire = Dust.NewDustPerfect(fireSpawnPosition, 267);
            brimstoneFire.velocity = fireDustVelocity;
            brimstoneFire.color = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.7f));
            brimstoneFire.scale = Main.rand.NextFloat(1.05f, 1.45f);
            brimstoneFire.fadeIn = 0.5f;
            brimstoneFire.noGravity = true;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SuicideBomberDemonExplosion>(), 0, 0f);
            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SuicideBomberDemonHostile>(), SupremeCalamitasBehaviorOverride.SuicideBomberDemonDamage, 0f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
