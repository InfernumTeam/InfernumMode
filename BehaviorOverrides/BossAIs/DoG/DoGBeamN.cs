using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGBeamN : ModProjectile
    {
        public ref float Time => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Portal Laser");
            Main.projFrames[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = 6;
            projectile.height = 6;
            projectile.hostile = true;
            projectile.scale = 2f;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = 1;
            projectile.timeLeft = 960;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            Lighting.AddLight(projectile.Center, 0f, 0.2f, 0.3f);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Home in on the closest target after a small period of time.
            if (Time < 120f && Time > 30f)
            {
                float speed = projectile.velocity.Length();
                Vector2 idealVelocity = projectile.SafeDirectionTo(Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center) * speed;
                projectile.velocity = (projectile.velocity * 20f + idealVelocity) / 21f;
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
            }
            Time++;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 100);
    }
}
