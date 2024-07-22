using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class WaterSpear : ModProjectile
    {
        public ref float StartingYPosition => ref Projectile.ai[0];

        public const float Gravity = 0.35f;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Water Spear");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.64f);

            Projectile.Opacity = Sin(Pi * Projectile.timeLeft / 360f) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            // Fall.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + Gravity, -34f, 14f);
            Projectile.tileCollide = Projectile.Center.Y >= StartingYPosition + 72f;

            // Frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
                Projectile.frameCounter = 0;
            }
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.75f ? null : false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Water);
                dust.velocity = Main.rand.NextVector2Circular(5f, 5f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle sourceRectangle = texture.Frame(1, Main.projFrames[Projectile.type], frameY: Projectile.frame);
            Vector2 origin = sourceRectangle.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 6f).ToRotationVector2() * 3f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, sourceRectangle, new Color(1f, 1f, 1f, 0f) * Projectile.Opacity * 0.65f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, sourceRectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
