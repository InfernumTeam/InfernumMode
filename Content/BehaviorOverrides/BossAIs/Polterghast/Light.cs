using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast
{
    public class Light : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Light");

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 40;
            Projectile.penetrate = -1;
        }

        public override void AI() => Time++;

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 origin = new(66f, 86f);
            Vector2 drawPosition = new(Projectile.Center.X - Main.screenPosition.X, Main.screenHeight + 10f);
            Vector2 scale = new(0.75f, 1.9f);
            lightColor = new(205, 109, 155);
            Color coloredLight = lightColor;
            float completion = 0f;
            if (Time < 10f)
                completion = Utils.GetLerpValue(0f, 10f, Time, true);
            else if (Time < 40f)
                completion = 1f + Utils.GetLerpValue(10f, 40f, Time, true);

            Vector2 scaleFactor1 = new(1f, 1f);
            Vector2 scaleFactor2 = new Vector2(0.8f, 2f) * 0.4f;
            if (completion < 1f)
                scaleFactor1.X *= completion;

            scale *= completion;
            if (completion < 1f)
            {
                lightColor *= completion;
                coloredLight *= completion;
            }
            if (completion > 1.5f)
            {
                float fade = Utils.GetLerpValue(2f, 1.5f, completion, true);
                lightColor *= fade;
                coloredLight *= fade;
            }
            lightColor *= 0.7f;
            coloredLight *= 0.7f;
            Texture2D texture7 = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Polterghast/Light").Value;

            scale.X *= Main.screenWidth / texture7.Width;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(texture7, drawPosition, null, lightColor, 0f, new(265f, 354f), scale * scaleFactor1 * new Vector2(0.6f, 0.45f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture7, drawPosition, null, coloredLight, 0f, new(265f, 354f), scale * scaleFactor2 * new Vector2(0.6f, 0.45f), SpriteEffects.None, 0f);

            Texture2D lightTexture = TextureAssets.Extra[59].Value;
            Main.spriteBatch.Draw(lightTexture, drawPosition, null, lightColor, 0f, origin, scale * scaleFactor1 * new Vector2(1f, 0.3f), SpriteEffects.None, 0f);
            Main.spriteBatch.ResetBlendState();

            return false;
        }
    }
}
