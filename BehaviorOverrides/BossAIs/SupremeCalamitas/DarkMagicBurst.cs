using CalamityMod;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkMagicBurst : ModProjectile
    {
        public ref float DartCountFactor => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Burst");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (DartCountFactor <= 0f)
                DartCountFactor = 1f;

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            Utilities.CreateGenericDustExplosion(Projectile.Center, 242, 10, 7f, 1.25f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            for (int i = 0; i < DartCountFactor * 10f; i++)
            {
                Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center).RotatedByRandom(0.77f) * Projectile.velocity.Length() * Main.rand.NextFloat(0.65f, 0.85f);
                int dart = Utilities.NewProjectileBetter(Projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), 540, 0f);
                if (Main.projectile.IndexInRange(dart))
                {
                    Main.projectile[dart].ai[0] = 1f;
                    Main.projectile[dart].tileCollide = false;
                    Main.projectile[dart].netUpdate = true;
                }
            }
        }
    }
}
