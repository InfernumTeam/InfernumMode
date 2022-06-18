using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class NettlevineArenaSeparator : ModProjectile
    {
        public Vector2 StartingPosition
        {
            get
            {
                if (projectile.ai[0] == 0f && projectile.ai[1] == 0f)
                {
                    projectile.ai[0] = projectile.Center.X;
                    projectile.ai[1] = projectile.Center.Y;
                    projectile.netUpdate = true;
                }
                return new Vector2(projectile.ai[0], projectile.ai[1]);
            }
        }
        public override void SetStaticDefaults() => DisplayName.SetDefault("Nettlevine");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 10;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 660;
            projectile.penetrate = -1;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            if (projectile.timeLeft < 480)
                projectile.velocity *= 0.985f;
            else
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.Opacity = Utils.InverseLerp(660f, 620f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 30f, projectile.timeLeft, true);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tipTexture = Main.projectileTexture[projectile.type];
            Texture2D body1Texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Plantera/NettlevineArenaSeparatorBody1");
            Texture2D body2Texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Plantera/NettlevineArenaSeparatorBody2");
            Vector2 bodyOrigin = body1Texture.Size() * new Vector2(0.5f, 1f);
            Vector2 tipOrigin = tipTexture.Size() * new Vector2(0.5f, 1f);
            Vector2 currentDrawPosition = StartingPosition;
            Color drawColor = projectile.GetAlpha(Color.White);

            int fuck = 0;
            while (!projectile.WithinRange(currentDrawPosition, 36f))
            {
                Texture2D textureToUse = fuck % 2 == 0 ? body1Texture : body2Texture;
                spriteBatch.Draw(textureToUse, currentDrawPosition - Main.screenPosition, null, drawColor, projectile.rotation, bodyOrigin, projectile.scale, SpriteEffects.None, 0f);
                currentDrawPosition += (projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * body1Texture.Height;
                fuck++;
            }

            spriteBatch.Draw(tipTexture, currentDrawPosition - Main.screenPosition, null, drawColor, projectile.rotation, tipOrigin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = StartingPosition;
            Vector2 end = projectile.Center;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 8f, ref _);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
    }
}
