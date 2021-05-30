using CalamityMod;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.DoG
{
    public class EssenceExplosion : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = 260;
            projectile.height = 260;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 75;
            projectile.scale = 0.15f;
        }

        public override void AI()
        {
            projectile.scale *= 1.042f;
            projectile.Opacity = Utils.InverseLerp(5f, 36f, projectile.timeLeft, true);
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Texture2D lightTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/PhotovisceratorLight");

            for (int i = 0; i < 36; i++)
            {
                Vector2 lightDrawPosition = drawPosition + (MathHelper.TwoPi * i / 36f + Main.GlobalTime * 5f).ToRotationVector2() * projectile.scale * 20f;
                Color lightBurstColor = Color.Lerp(Color.Fuchsia, Color.White, 0.4f) * projectile.Opacity * 0.07f;
                lightBurstColor.A = 0;

                spriteBatch.Draw(lightTexture, lightDrawPosition, null, lightBurstColor, 0f, lightTexture.Size() * 0.5f, projectile.scale * 2.5f, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
