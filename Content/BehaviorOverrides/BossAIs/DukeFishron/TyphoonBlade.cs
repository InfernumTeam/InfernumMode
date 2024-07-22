using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DukeFishron
{
    public class TyphoonBlade : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SharknadoBolt}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Typhoon Blade");
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 56;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 270;
            Projectile.penetrate = -1;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Projectile.type];

            Time++;
            Projectile.rotation += 0.4f * (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Time, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);

            if (Projectile.timeLeft < 90f)
            {
                if (Projectile.velocity.Length() < 15f)
                    Projectile.velocity *= 1.016f;
            }
            else if (Time > 40f)
            {
                float oldSpeed = Projectile.velocity.Length();
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = (Projectile.velocity * 49f + Projectile.SafeDirectionTo(target.Center) * oldSpeed) / 50f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * oldSpeed;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
