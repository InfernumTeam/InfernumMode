using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class ElectricOrb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 88;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 90;
            Projectile.Opacity = 0f;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(90f, 65f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.velocity *= 0.98f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 56) * Projectile.Opacity;
        }

        public override void Kill(int timeLeft)
        {
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Release a lightning bolt and circle of sparks.
            Vector2 lightningVelocity = Projectile.SafeDirectionTo(target.Center) * (Projectile.Distance(target.Center) / 450f + 6.1f);
            int arc = Utilities.NewProjectileBetter(Projectile.Center, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 255, 0f);
            if (Main.projectile.IndexInRange(arc))
            {
                Main.projectile[arc].ai[0] = lightningVelocity.ToRotation();
                Main.projectile[arc].ai[1] = Main.rand.Next(100);
                Main.projectile[arc].tileCollide = false;
            }

            float initialSparkRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 6f + initialSparkRotation).ToRotationVector2() * 15f;
                Utilities.NewProjectileBetter(Projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 255, 0f);
            }
        }
    }
}
