using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class PressureSandnado : ModProjectile
    {
        public const int Lifetime = 300;

        public const float HorizontalCollisionAreaFactor = 0.2f;

        public ref float Time => ref Projectile.ai[0];

        public ref float StuckTimer => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/TornadoProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Tornado");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 800;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (StuckTimer >= 120f && Time < Lifetime - 15f)
                Time = Lifetime - 15f;

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();

            Vector2 top = Projectile.Top;
            Vector2 bottom = Projectile.Bottom;
            Vector2 center = Vector2.Lerp(top, bottom, 0.5f);
            Vector2 dustSpawnArea = Vector2.UnitY * (bottom.Y - top.Y);
            dustSpawnArea.X = dustSpawnArea.Y * HorizontalCollisionAreaFactor;
            int horizontalCollisionCheckArea = 16;
            int checkHeight = (int)(Projectile.height * 0.75f);

            // Do collision checks.
            Vector2 position = new(Projectile.Center.X - horizontalCollisionCheckArea / 2, Projectile.position.Y + Projectile.height - checkHeight);
            if (Collision.SolidCollision(position, horizontalCollisionCheckArea, checkHeight) ||
                Collision.WetCollision(position, horizontalCollisionCheckArea, checkHeight))
            {
                if (Projectile.velocity.Y > 0f)
                    Projectile.velocity.Y = 0f;

                if (Projectile.velocity.Y > -4f)
                    Projectile.velocity.Y -= 2f;
                else
                {
                    Projectile.velocity.Y -= 4f;
                    StuckTimer += 2f;
                }

                if (Projectile.velocity.Y < -16f)
                    Projectile.velocity.Y = -16f;
            }
            else
            {
                StuckTimer--;
                if (StuckTimer < 0f)
                    StuckTimer = 0f;

                if (Projectile.velocity.Y < 0f)
                    Projectile.velocity.Y = 0f;

                if (Projectile.velocity.Y < 4f)
                    Projectile.velocity.Y += 2f;
                else
                    Projectile.velocity.Y += 4f;

                if (Projectile.velocity.Y > 16f)
                    Projectile.velocity.Y = 16f;
            }

            // Create dust.
            if (Time < Lifetime - 30f)
            {
                float fuck = Main.rand.NextFloat();
                Vector2 dustSpawnOffsetFactor = new(Main.rand.NextFloat(0.1f, 1f), MathHelper.Lerp(-1f, 0.9f, fuck));
                dustSpawnOffsetFactor.X *= MathHelper.Lerp(2.2f, 0.6f, fuck);
                dustSpawnOffsetFactor.X *= -1f;
                Vector2 dustSpawnPosition = center + dustSpawnArea * dustSpawnOffsetFactor * 0.5f + new Vector2(6f, 10f);

                Dust sand = Dust.NewDustDirect(dustSpawnPosition, 0, 0, 274, 0f, 0f, 0, default, 1f);
                sand.position = dustSpawnPosition;
                sand.fadeIn = 1.3f;
                sand.scale = 0.87f;
                sand.alpha = 211;
                if (dustSpawnOffsetFactor.X > -1.2f)
                    sand.velocity.X = Main.rand.NextFloat(1f, 2f);

                sand.noGravity = true;
                sand.velocity.Y = Main.rand.NextFloat() * -0.5f - 1.3f;
                sand.velocity.X += Projectile.velocity.X * 2.1f;
                sand.color = Color.Yellow;
                sand.noLight = true;
            }

            Vector2 dustSpawnTopLeft = Projectile.Bottom + new Vector2(-25f, -25f);
            for (int k = 0; k < 5; k++)
            {
                Dust sand = Dust.NewDustDirect(dustSpawnTopLeft, 50, 25, 32, Projectile.velocity.X, -2f, 100, default, 1f);
                sand.fadeIn = 1.1f;
                sand.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float generalOpacity = Utils.GetLerpValue(0f, 30f, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - 60f, Time, true) * 1.8f;
            Vector2 top = Projectile.Top;
            Vector2 bottom = Projectile.Bottom;
            Vector2 tornadoArea = Vector2.UnitY * (bottom.Y - top.Y);
            tornadoArea.X = tornadoArea.Y * HorizontalCollisionAreaFactor;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float baseRotation = MathHelper.TwoPi / -20f * Time * (Projectile.velocity.X <= 0f).ToDirectionInt();
            SpriteEffects direction = Projectile.velocity.X > 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float currentHeightOfTornado = 0f;
            float heightPerTornadoPiece = 5.01f + Time / 150f * -0.9f;
            if (heightPerTornadoPiece < 4.11f)
                heightPerTornadoPiece = 4.11f;
            heightPerTornadoPiece *= 1.6f;

            Color baseColor = new(160, 140, 100, 127);
            Color baseAfterimageColor = new(255, 170, 85, 127);
            float wrappedTime = Time % 60f;
            if (wrappedTime < 30f)
                baseAfterimageColor *= Utils.GetLerpValue(22f, 30f, wrappedTime, true);
            else
                baseAfterimageColor *= Utils.GetLerpValue(38f, 30f, wrappedTime, true);

            for (float y = (int)bottom.Y; y > (int)top.Y; y -= heightPerTornadoPiece)
            {
                currentHeightOfTornado += heightPerTornadoPiece;
                float verticalCompletionRatio = currentHeightOfTornado / tornadoArea.Y;
                float heightBasedRotation = currentHeightOfTornado * MathHelper.TwoPi / -10f;
                if (Projectile.velocity.X > 0f)
                    heightBasedRotation *= -1f;

                float scaleAdditive = verticalCompletionRatio - 0.35f;
                Vector2 drawPosition = new Vector2(bottom.X, y) - Main.screenPosition;
                Color color = Color.Lerp(Color.Transparent, baseColor, verticalCompletionRatio * 2f);
                if (verticalCompletionRatio > 0.5f)
                    color = Color.Lerp(Color.Transparent, baseColor, 2f - verticalCompletionRatio * 2f);

                color.A = (byte)(color.A * 0.2f);
                color *= generalOpacity;
                if (baseAfterimageColor != Color.Transparent)
                {
                    Color afterimageColor = Color.Lerp(Color.Transparent, baseAfterimageColor, verticalCompletionRatio * 2f);
                    if (verticalCompletionRatio > 0.5f)
                        afterimageColor = Color.Lerp(Color.Transparent, baseAfterimageColor, 2f - verticalCompletionRatio * 2f);

                    afterimageColor.A = (byte)(afterimageColor.A * 0.5f);
                    afterimageColor *= generalOpacity;
                    Main.spriteBatch.Draw(texture, drawPosition, null, afterimageColor, baseRotation + heightBasedRotation, origin, (1f + scaleAdditive) * 0.8f, direction, 0f);
                }

                Main.spriteBatch.Draw(texture, drawPosition, null, color, baseRotation + heightBasedRotation, origin, 1f + scaleAdditive, direction, 0f);
            }
            return false;
        }
    }
}
