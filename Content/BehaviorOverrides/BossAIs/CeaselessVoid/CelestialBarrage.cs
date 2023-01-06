using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CelestialBarrage : ModProjectile
    {
        public float Power => Projectile.ai[1];

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Otherwordly Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            // Home in on the target before accelerating.
            if (Time <= 45f)
            {
                float homeSpeed = Power * 8.5f + 21.5f;
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (!Projectile.WithinRange(target.Center, 200f))
                    Projectile.velocity = (Projectile.velocity * 14f + Projectile.SafeDirectionTo(target.Center) * homeSpeed) / 15f;
            }
            else if (Projectile.velocity.Length() < 24f)
                Projectile.velocity *= Power * 0.025f + 1.02f;

            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override Color? GetAlpha(Color lightColor) => new Color(255, 108, 50, 0) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            for (int i = 0; i < 5; i++)
                Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            return false;
        }
    }
}
