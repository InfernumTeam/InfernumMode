using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Golem
{
    public class GroundFireCrystal : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fire Crystal");

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 270;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] == 0f)
            {
                Utilities.NewProjectileBetter(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<FistBulletTelegraph>(), 0, 0f);
                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.03f, 0f, 1f);

            if (Projectile.Opacity >= 1f)
                Projectile.velocity = (Projectile.velocity * 1.05f).ClampMagnitude(5f, 36f);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle rectangle = new(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * .5f;
            Color drawColor = Projectile.GetAlpha(lightColor);
            drawColor = Color.Lerp(drawColor, Color.Yellow, 0.5f);
            drawColor.A /= 7;

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, rectangle, drawColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 drawOffset = Projectile.velocity * -i * 0.6f;
                Color afterimageColor = drawColor * (1f - i / 3f);
                Main.spriteBatch.Draw(texture, Projectile.Center + drawOffset - Main.screenPosition, rectangle, afterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            return false;
        }
    }
}
