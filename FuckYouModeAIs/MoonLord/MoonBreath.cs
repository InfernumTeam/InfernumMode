using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class MoonBreath : ModProjectile
    {
        const int maxTimeLeft = 300;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Breath");
        }

        public override void SetDefaults()
        {
            projectile.width = 28;
            projectile.height = 24;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = maxTimeLeft;
        }
        public override void AI()
        {
            if (projectile.timeLeft > maxTimeLeft - 120)
            {
                projectile.velocity = (projectile.velocity * 38f + projectile.DirectionTo(Main.player[(int)projectile.ai[0]].Center) * 6f) / 39f;
            }
            else
            {
                projectile.velocity *= 0.96f;
            }
            if (projectile.timeLeft < 81)
            {
                projectile.alpha += 3;
            }
            projectile.rotation += projectile.velocity.X * 0.12f;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Main.spriteBatch.Draw(ModContent.GetTexture("InfernumMode/FuckYouModeAIs/MoonBreath"),
                   projectile.Center - Main.screenPosition, null, Color.White, projectile.rotation, projectile.Size / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
