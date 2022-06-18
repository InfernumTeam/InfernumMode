using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Skeletron
{
    public class SpinningFireball : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shadowflame Fireball");

        public override void SetDefaults()
        {
            projectile.scale = 1.3f;
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.3f;
            projectile.velocity = projectile.velocity.RotatedBy(MathHelper.TwoPi / 120f);

            if (Main.dedServ)
                return;

            Dust cursedFlame = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width, projectile.height) * 0.5f, 173);
            cursedFlame.velocity = Vector2.UnitY.RotatedBy(projectile.velocity.ToRotation()) * Main.rand.NextFloat(1.5f, 2.3f);
            cursedFlame.scale = Main.rand.NextFloat(0.7f, 0.8f);
            cursedFlame.fadeIn = 0.6f;
            cursedFlame.noGravity = true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            spriteBatch.Draw(texture, drawPosition, null, Color.White, projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
