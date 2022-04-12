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
            Projectile.scale = 1.3f;
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * 0.3f;
            Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.TwoPi / 120f);

            if (Main.dedServ)
                return;

            Dust cursedFlame = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f, 173);
            cursedFlame.velocity = Vector2.UnitY.RotatedBy(Projectile.velocity.ToRotation()) * Main.rand.NextFloat(1.5f, 2.3f);
            cursedFlame.scale = Main.rand.NextFloat(0.7f, 0.8f);
            cursedFlame.fadeIn = 0.6f;
            cursedFlame.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.Draw(texture, drawPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
