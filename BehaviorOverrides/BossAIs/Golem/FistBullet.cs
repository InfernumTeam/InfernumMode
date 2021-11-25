using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    class FistBullet : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fist Bullet");

        public override void SetDefaults()
        {
            projectile.width = 40;
            projectile.height = 40;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
        }

        public override bool PreAI()
        {
            if (projectile.Infernum().ExtraAI[0] < 60f)
            {
                if (Main.player.IndexInRange((int)projectile.Infernum().ExtraAI[2]))
                {
                    Vector2 target = Main.player[(int)projectile.Infernum().ExtraAI[2]].Center;
                    float rotation = -(projectile.rotation + MathHelper.Pi - (projectile.DirectionTo(target).ToRotation() + MathHelper.Pi));
                    projectile.rotation = MathHelper.WrapAngle(projectile.rotation + MathHelper.Clamp(rotation, -MathHelper.ToRadians(10), MathHelper.ToRadians(10)));
                }
            }
            else if (projectile.Infernum().ExtraAI[0] == 60f)
            {
                if (Main.player.IndexInRange((int)projectile.Infernum().ExtraAI[2]))
                {
                    Vector2 target = Main.player[(int)projectile.Infernum().ExtraAI[2]].Center;
                    projectile.rotation = projectile.DirectionTo(target).ToRotation();

                    projectile.velocity = projectile.rotation.ToRotationVector2() * (projectile.Distance(target) / 20f);
                }
            }

            projectile.Infernum().ExtraAI[0]++;
            projectile.direction = MathHelper.WrapAngle(projectile.rotation + MathHelper.PiOver2) >= 0 ? 1 : -1;
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects flipped = SpriteEffects.None;
            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * .5f;
            Color drawColor = projectile.GetAlpha(lightColor);

            Main.spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, rectangle, drawColor, projectile.rotation, origin, projectile.scale, flipped, 0f);
            return false;
        }
    }
}
