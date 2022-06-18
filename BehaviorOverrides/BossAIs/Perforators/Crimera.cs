using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class Crimera : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimera");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            projectile.width = 44;
            projectile.height = 44;
            projectile.ignoreWater = true;
            projectile.timeLeft = 490;
            projectile.scale = 1f;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI() => projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Color drawColor = Color.Yellow;
            drawColor.A = 0;
            drawColor *= 0.5f;

            Utilities.DrawAfterimagesCentered(projectile, drawColor, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return true;
        }
    }
}
