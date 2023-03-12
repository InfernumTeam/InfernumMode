using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.KingSlime
{
    public class Shuriken : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shuriken");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * 0.4f;
            Projectile.tileCollide = Projectile.timeLeft < 90;

            if (Projectile.velocity.Length() < 9.5f)
                Projectile.velocity *= 1.0145f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D shurikenTexture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D outlineTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/KingSlime/ShurikenOutline").Value;
            float pulseOutwardness = MathHelper.Lerp(2f, 3f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 2.5f) * 0.5f + 0.5f);
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * pulseOutwardness;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                Color innerAfterimageColor = Color.Wheat * Projectile.Opacity * 0.5f;
                innerAfterimageColor.A = 0;

                Color outerAfterimageColor = Color.Lerp(Color.DarkGray, Color.Black, 0.66f) * Projectile.Opacity * 0.5f;
                Main.spriteBatch.Draw(shurikenTexture, drawPosition, null, outerAfterimageColor, Projectile.rotation, shurikenTexture.Size() * 0.5f, Projectile.scale * 1.085f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(shurikenTexture, drawPosition, null, innerAfterimageColor, Projectile.rotation, shurikenTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 outlineDrawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(outlineTexture, outlineDrawPosition, null, Color.White * Projectile.Opacity * 0.8f, Projectile.rotation, outlineTexture.Size() * 0.5f, Projectile.scale * 1.25f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(shurikenTexture, outlineDrawPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, outlineTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft) => Collision.HitTiles(Projectile.position, Projectile.velocity, 24, 24);
    }
}
