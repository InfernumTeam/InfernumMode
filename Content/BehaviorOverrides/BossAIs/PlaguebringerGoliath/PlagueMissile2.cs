using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueMissile2 : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];
        public Player Target => Main.player[(int)Projectile.ai[0]];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Missile");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.Hitbox.Intersects(Target.Hitbox))
                Projectile.Kill();

            // Emit smoke effects.
            RedirectingPlagueMissile.EmitSmoke(Projectile);

            Projectile.tileCollide = Projectile.Center.Y > Target.Center.Y;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(35f, 35f), 89);
                dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                dust.scale = Main.rand.NextFloat(1.1f, 1.35f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>(Texture.Replace("2", string.Empty) + "Glowmask").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Draw afterimages.
            for (int i = 0; i < 6; i++)
            {
                Vector2 afterimageOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * i * -16f;
                Color afterimageColor = Color.Lime * (1f - i / 6f) * 0.7f;
                afterimageColor.A = 0;
                Main.spriteBatch.Draw(texture, drawPosition + afterimageOffset, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, lightColor, 4f);
            Main.spriteBatch.Draw(glowmask, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 0.8f;
    }
}
