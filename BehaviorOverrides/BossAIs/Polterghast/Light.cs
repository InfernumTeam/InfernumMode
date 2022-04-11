using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
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

        public override bool PreDraw(SpriteBatch spriteBatch, Color _)
        {
            Vector2 origin = new(66f, 86f);
            Vector2 drawPosition = new(Main.screenWidth * 0.5f, Main.screenHeight + 10f);
            Vector2 scale = new(1.4f, 1.6f);
            Color lightColor = new(205, 10, 205, 0);
            Color coloredLight = new(255, 180, 255, 0);
            float completion = 0f;
            if (Time < 30f)
                completion = Utils.GetLerpValue(0f, 30f, Time, true);
            else if (Time < 40f)
                completion = 1f + Utils.GetLerpValue(30f, 40f, Time, true);

            Vector2 scaleFactor1 = new(1f, 1f);
            Vector2 scaleFactor2 = new(0.8f, 2f);
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
            lightColor *= 0.42f;
            coloredLight *= 0.42f;
            Texture2D texture7 = Main.extraTexture[60];

            scale.X *= Main.screenWidth / texture7.Width;
            Main.spriteBatch.Draw(texture7, drawPosition, null, lightColor, 0f, origin, scale * scaleFactor1, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture7, drawPosition, null, coloredLight, 0f, origin, scale * scaleFactor2, SpriteEffects.None, 0f);

            Texture2D lightTexture = Main.extraTexture[59];
            Main.spriteBatch.Draw(lightTexture, drawPosition, null, lightColor, 0f, origin, scale * scaleFactor1 * new Vector2(1f, 0.3f), SpriteEffects.None, 0f);

            return false;
        }
    }
}
