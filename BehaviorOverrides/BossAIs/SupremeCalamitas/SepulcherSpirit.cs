using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
	public class SepulcherSpirit : ModProjectile
    {
        public ref float TimeCountdown => ref Projectile.ai[0];
        public ref float SpiritHue => ref Projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sepulcher Spirit");
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            Projectile.Opacity = Utils.GetLerpValue(0f, 24f, TimeCountdown, true);
            TimeCountdown--;

            // Attempt to hover above the target.
            Vector2 hoverDestination = Main.player[Projectile.owner].Center + SupremeCalamitasBehaviorOverride.SepulcherSpawnOffset;
            Vector2 idealVelocity = Projectile.SafeDirectionTo(hoverDestination) * 25f;
            if (!Projectile.WithinRange(hoverDestination, 100f))
                Projectile.velocity = (Projectile.velocity * 19f + idealVelocity) / 20f;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (TimeCountdown <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Main.hslToRgb(SpiritHue, 1f, 0.5f);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, Utilities.ProjTexture(Projectile.type), false);
            return false;
        }
    }
}
