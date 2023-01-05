using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace InfernumMode.Projectiles
{
    public class SnowflakeCinder : BaseCinderProjectile
    {
        private Color? DrawColor = null;
        private readonly Color[] PossibleColors =
        {
            new(221,254,255),
            new(184,237,255),
            new(124,147,211)
        };
        private float RotationPerFrame = 0;
        public override int NumberOfFrames => 4;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Snowflake");
        }
        public override void Initialize()
        {
            DrawColor = PossibleColors[Main.rand.Next(0, PossibleColors.Length)];
            RotationPerFrame = Main.rand.NextFloat(0, 0.25f);
        }
        public override void AI()
        {
            Projectile.rotation += RotationPerFrame;
            base.AI();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (DrawColor.HasValue)
            {
                Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
                Rectangle sourceRectangle = texture.Frame(1, Main.projFrames[Projectile.type], frameY: Projectile.frame);
                Vector2 origin = texture.Size() * 0.5f;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, DrawColor.Value, Projectile.rotation, origin, Projectile.scale * 0.5f, 0, 0);
            }
            return false;
        }
    }
}
