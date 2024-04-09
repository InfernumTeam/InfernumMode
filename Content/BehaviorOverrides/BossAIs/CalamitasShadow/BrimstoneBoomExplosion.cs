using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class BrimstoneBoomExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Skies/XerocLight";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 520;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.MaxUpdates = 3;
            Projectile.scale = 0.2f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Emit a strong white light.
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.5f);

            // Determine frames. Once the maximum frame is reached the projectile dies.
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 8 == 7)
                Projectile.frame++;
            if (Projectile.frame >= 18)
                Projectile.Kill();

            // Exponentially expand.
            Projectile.scale *= 1.013f;
            Projectile.Opacity = Utils.GetLerpValue(5f, 36f, Projectile.timeLeft, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/TerratomereExplosion").Value;
            Texture2D lightTexture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(3, 6, Projectile.frame / 6, Projectile.frame % 6);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 36; i++)
            {
                Vector2 lightDrawPosition = drawPosition + (TwoPi * i / 36f + Main.GlobalTimeWrappedHourly * 5f).ToRotationVector2() * Projectile.scale * 12f;
                Color lightBurstColor = LumUtils.MulticolorLerp(Projectile.timeLeft / 144f, Color.OrangeRed, Color.Yellow);
                lightBurstColor = Color.Lerp(lightBurstColor, Color.White, 0.4f) * Projectile.Opacity * 0.184f;
                Main.spriteBatch.Draw(lightTexture, lightDrawPosition, null, lightBurstColor, 0f, lightTexture.Size() * 0.5f, Projectile.scale * 1.32f, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(texture, drawPosition, frame, Color.Yellow, 0f, origin, 1.4f, SpriteEffects.None, 0);
            Main.spriteBatch.ResetBlendState();
            return false;
        }
    }
}
