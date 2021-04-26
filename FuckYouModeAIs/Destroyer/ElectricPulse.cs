using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Destroyer
{
    public class ElectricPulse : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Electric Pulse");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = 80;
            projectile.height = 80;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 150;
            projectile.Opacity = 0f;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, 0f, 0.95f, 1.15f);
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 4 % Main.projFrames[projectile.type];

            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.05f, 0f, 1f);
            projectile.tileCollide = projectile.timeLeft < 100;

            // Accelerate over time.
            if (projectile.velocity.Length() < 24f)
                projectile.velocity *= 1.01f;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 12; i++)
            {
                Vector2 arcVelocity = Main.rand.NextVector2Circular(2.5f, 2.5f);

                int arc = Utilities.NewProjectileBetter(projectile.Center, arcVelocity, ModContent.ProjectileType<ElectricArc>(), 130, 0f);
                Main.projectile[arc].scale = Main.rand.NextFloat(1f, 1.3f);
                Main.projectile[arc].ai[0] = Main.rand.Next(100);
                Main.projectile[arc].ai[1] = arcVelocity.ToRotation();
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override bool CanDamage() => false;
    }
}
