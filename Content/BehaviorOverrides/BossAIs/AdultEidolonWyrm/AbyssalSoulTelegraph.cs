using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AbyssalSoulTelegraph : ModProjectile, IAboveWaterProjectileDrawer
    {
        public PrimitiveTrailCopy TelegraphDrawer
        {
            get;
            set;
        } = null;

        public Color StreakBaseColor => Color.Lerp(Color.DarkBlue, Color.MediumPurple, Projectile.ai[0] * 0.6f + 0.3f);

        public Vector2[] TelegraphPoints;

        public static int Lifetime => 96;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Abyssal Telegraph");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TelegraphPoints.Length);
            for (int i = 0; i < TelegraphPoints.Length; i++)
                writer.WritePackedVector2(TelegraphPoints[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int telegraphPointCount = reader.ReadInt32();

            TelegraphPoints = new Vector2[telegraphPointCount];
            for (int i = 0; i < telegraphPointCount; i++)
                TelegraphPoints[i] = reader.ReadPackedVector2();
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(Lifetime, Lifetime - 6f, Projectile.timeLeft) * Utils.GetLerpValue(0f, 11f, Projectile.timeLeft, true) * 0.5f;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.Cyan, Color.DarkSlateBlue, Projectile.ai[0]) with { A = 100 } * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            // The total number of lines to draw.
            int totalDrawPoints = TelegraphPoints.Length;

            Texture2D lineTexture = InfernumTextureRegistry.Pixel.Value;

            // Loop through the total number of draw points.
            int startingIteration = (Lifetime - Projectile.timeLeft) / 2 + 2;
            if (startingIteration <= 0)
                startingIteration = 1;

            for (int i = startingIteration; i < totalDrawPoints; i++)
            {
                // Get the direction between the two points.
                Vector2 direction = TelegraphPoints[i - 1] - TelegraphPoints[i];

                // Get the length of this. This doesn't fully connect normally so adding 0.5 to the length is a shitty
                // hack to make them work. However, this means you cannot use additive drawing due to the overlap being visible.
                float length = direction.Length() + 0.5f;

                // Use this to create a rectangle.
                Rectangle rectangle = new(0, 0, (int)length, 4);

                // Set the color of the line.
                Color lineColor = Color.Lerp(Color.DarkTurquoise, StreakBaseColor, Projectile.Opacity) * 1.3f;

                // Make it fade out for the last bit.
                if (totalDrawPoints - i <= 20)
                {
                    float interpolant = ((float)i - (totalDrawPoints - 20f)) / (totalDrawPoints - (totalDrawPoints - 20f));
                    lineColor = Color.Lerp(lineColor, Color.Transparent, interpolant);
                }
                lineColor *= Projectile.Opacity;

                // Draw the line.
                spriteBatch.Draw(lineTexture, TelegraphPoints[i - 1] - Main.screenPosition, rectangle, lineColor, direction.ToRotation(), rectangle.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
