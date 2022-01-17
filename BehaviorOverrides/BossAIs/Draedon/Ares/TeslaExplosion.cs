using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class TeslaExplosion : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 520;
            projectile.friendly = true;
            projectile.ignoreWater = false;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 150;
            projectile.extraUpdates = 2;
            projectile.scale = 0.35f;
        }

        public override void AI()
        {
            if (projectile.ai[0] != 0f)
            {
                projectile.MaxUpdates = 8;
                projectile.ai[0] = 0f;
            }

            Lighting.AddLight(projectile.Center, Color.White.ToVector3() * 1.5f);
            projectile.frameCounter++;
            if (projectile.frameCounter % 8 == 7)
                projectile.frame++;

            if (projectile.frame >= 18)
                projectile.Kill();
            projectile.scale *= 1.0115f;
            projectile.Opacity = Utils.InverseLerp(5f, 36f, projectile.timeLeft, true);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D lightTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/PhotovisceratorLight");
            Rectangle frame = texture.Frame(3, 6, projectile.frame / 6, projectile.frame % 6);
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 36; i++)
            {
                Vector2 lightDrawPosition = drawPosition + (MathHelper.TwoPi * i / 36f + Main.GlobalTime * 5f).ToRotationVector2() * projectile.scale * 20f;
                Color lightBurstColor = Color.Lerp(Color.Cyan, Color.White, 0.6f) * projectile.Opacity * 0.04f;
                lightBurstColor.A = 0;
                spriteBatch.Draw(lightTexture, lightDrawPosition, null, lightBurstColor, 0f, lightTexture.Size() * 0.5f, projectile.scale * 4.5f, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, frame, Color.White, 0f, origin, 1.6f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
