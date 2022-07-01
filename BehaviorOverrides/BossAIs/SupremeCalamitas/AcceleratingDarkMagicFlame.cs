using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class AcceleratingDarkMagicFlame : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic Flame");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Accelerate quickly.
            if (Projectile.velocity.Length() < 16f)
                Projectile.velocity *= 1.019f;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.Center - Vector2.One * 12f, 6, 6, 267);
                fire.color = Color.Lerp(Color.Fuchsia, Color.Orange, Main.rand.NextFloat());
                fire.scale = Main.rand.NextFloat(1f, 1.3f);
                fire.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1, TextureAssets.Projectile[Projectile.type].Value, false);
            return false;
        }
    }
}
