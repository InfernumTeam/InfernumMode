using InfernumMode.Common.Graphics;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class CircleCenterTelegraph : ModProjectile, IAboveWaterProjectileDrawer
    {
        public override string Texture => "CalamityMod/Skies/XerocLight";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 50f, Projectile.timeLeft, true);
            Projectile.scale = Utils.GetLerpValue(150f, 125f, Projectile.timeLeft, true) * 1.5f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.scale * 135f);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.45f;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            int drawCount = InfernumConfig.Instance.ReducedGraphicsConfig ? 1 : 3;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Color explosionColor = Color.LightCyan * Projectile.Opacity * 0.65f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < drawCount; i++)
                Main.spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, Projectile.scale + i * 0.1f, SpriteEffects.None, 0f);

            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
