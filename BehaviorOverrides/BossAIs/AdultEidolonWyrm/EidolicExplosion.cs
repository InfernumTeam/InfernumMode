using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class EidolicExplosion : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 150;
            projectile.extraUpdates = 1;
            projectile.scale = 0.15f;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.scale += 0.18f;
            projectile.Opacity = Utils.InverseLerp(150f, 142f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 40f, projectile.timeLeft, true);
            Lighting.AddLight(projectile.Center, Color.Cyan.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            for (int i = 0; i < 3; i++)
            {
                Color explosionColor = Color.Lerp(Color.White, Color.Cyan, i / 2f) * projectile.Opacity * 0.7f;
                spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(projectile.Center, targetHitbox, projectile.scale * 135f);
        }

        public override bool CanDamage() => projectile.Opacity > 0.45f;
    }
}
