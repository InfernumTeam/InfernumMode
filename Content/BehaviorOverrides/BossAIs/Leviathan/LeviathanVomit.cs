using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class LeviathanVomit : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Leviathan Vomit");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 270;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Determine opacity and rotation.
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 13f, Time, true);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
                Projectile.localAI[0] = 1f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            Lighting.AddLight(Projectile.Center, 0f, 0f, 0.5f * Projectile.Opacity);
            Time++;
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.6f;

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            LumUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
