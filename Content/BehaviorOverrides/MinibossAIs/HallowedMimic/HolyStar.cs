using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.HallowedMimic
{
    public class HolyStar : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Star");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Time++;

            Projectile.tileCollide = Time >= 105f;
            Projectile.rotation += Projectile.velocity.X * 0.05f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Time, true);

            // Release stardust.
            if (Main.rand.NextBool(10))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, default, 1.2f);
            if (Main.rand.NextBool(20))
                Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.position, Projectile.velocity * 0.2f, Main.rand.Next(16, 18), 1f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
