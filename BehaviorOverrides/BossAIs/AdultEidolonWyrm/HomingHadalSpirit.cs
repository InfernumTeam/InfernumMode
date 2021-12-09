using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class HomingHadalSpirit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hadal Spirit");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(300f, 290f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 15f, projectile.timeLeft, true);
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (!target.dead && projectile.timeLeft < 260f)
            {
                Vector2 idealVelocity = projectile.SafeDirectionTo(target.Center) * 15f;
                projectile.velocity = (projectile.velocity * 24f + idealVelocity) / 24f;
            }

            projectile.velocity = projectile.velocity.ClampMagnitude(3f, 20f);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
