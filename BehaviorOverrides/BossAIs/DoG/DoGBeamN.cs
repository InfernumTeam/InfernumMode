using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGBeamN : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Portal Laser");
            Main.projFrames[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.scale = 2f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 960;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Lighting.AddLight(Projectile.Center, 0f, 0.2f, 0.3f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Home in on the closest target after a small period of time.
            if (Time < 120f && Time > 30f)
            {
                float speed = Projectile.velocity.Length();
                Vector2 idealVelocity = Projectile.SafeDirectionTo(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center) * speed;
                Projectile.velocity = (Projectile.velocity * 20f + idealVelocity) / 21f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
            }
            Time++;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 100);
    }
}
