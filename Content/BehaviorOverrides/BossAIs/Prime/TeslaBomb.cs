using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class TeslaBomb : ModProjectile
    {
        public ref float Lifetime => ref Projectile.ai[0];
        public ref float Timer => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tesla Bomb");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            if (Timer > Lifetime)
                Projectile.Kill();

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.75f);
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.LightCyan, 0.7f);
            lightColor.A = 128;
            lightColor *= Projectile.Opacity;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 cloudShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.2f, 4f);
                Utilities.NewProjectileBetter(Projectile.Center + cloudShootVelocity * 3f, cloudShootVelocity, ModContent.ProjectileType<SmallElectricGasGloud>(), PrimeHeadBehaviorOverride.TeslaCloudDamage, 0f);
            }
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.alpha < 20;
    }
}
