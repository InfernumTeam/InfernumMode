using CalamityMod.Projectiles.Enemy;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class ToothBallSpikeBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ModContent.ProjectileType<TrilobiteSpike>();

        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectilePreDraw;

        public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            
            // Draw backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 4f;
                Color afterimageColor = new Color(1f, 1f, 1f, 0f) * 0.7f;
                Main.spriteBatch.Draw(texture, drawPosition + afterimageOffset, null, projectile.GetAlpha(afterimageColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}