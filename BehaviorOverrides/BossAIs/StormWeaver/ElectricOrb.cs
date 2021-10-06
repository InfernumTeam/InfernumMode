using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class ElectricOrb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 88;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = 90;
            projectile.Opacity = 0f;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(90f, 65f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.velocity *= 0.98f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 56) * projectile.Opacity;
        }

        public override void Kill(int timeLeft)
        {
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            Main.PlaySound(SoundID.DD2_KoboldExplosion, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Release a lightning bolt and circle of sparks.
            Vector2 lightningVelocity = projectile.SafeDirectionTo(target.Center) * (projectile.Distance(target.Center) / 450f + 6.1f);
            int arc = Utilities.NewProjectileBetter(projectile.Center, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 255, 0f);
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
                Utilities.NewProjectileBetter(projectile.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 255, 0f);
            }
        }
    }
}
