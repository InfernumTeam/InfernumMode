using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresBeamExplosion : ModProjectile
    {
        public ref float Identity => ref projectile.ai[0];
        public PrimitiveTrail LightningDrawer;
        public PrimitiveTrail LightningBackgroundDrawer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exoburst Explosion");
            Main.projFrames[projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            projectile.width = 68;
            projectile.height = 68;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(projectile.Center, 0.3f * projectile.Opacity, 0.3f * projectile.Opacity, 0.3f * projectile.Opacity);

            // Handle frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 4;

            // Die once the final frame is passed.
            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.Kill();
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

        public override Color? GetAlpha(Color lightColor) => Color.White;

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item93, projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into plasma sparks on death.
            for (int i = 0; i < 3; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(10f, 10f);
                Utilities.NewProjectileBetter(projectile.Center, sparkVelocity, ModContent.ProjectileType<ExoburstSpark>(), 550, 0f);
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
