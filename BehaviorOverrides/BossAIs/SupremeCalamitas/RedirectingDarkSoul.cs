using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedirectingDarkSoul : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Soul");
            Main.projFrames[Projectile.type] = 4;
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
            Projectile.timeLeft = 360;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Time, true);
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            // Slow down dramatically on the vertical axis and speed upon the horizontal one.
            Projectile.velocity.X *= 1.02f;
            Projectile.velocity.Y *= 0.96f;

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
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, Utilities.ProjTexture(Projectile.type), false);
            return false;
        }
    }
}
