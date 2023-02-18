using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.DrawLayers
{
    public class SoulDrivenHeadphonesLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.Infernum_Music().UsingHeadphones)
                return drawInfo.shadow <= 0f;
            return false;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var drawPlayer = drawInfo.drawPlayer;
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/Items/SoulDrivenHeadphones_Head").Value;

            // It is imperative drawInfo.Position and not drawPlayer.position is used, or else the layer will break on the player select and the map (in the case of a head layer).
            Vector2 headDrawPosition = drawInfo.Position - Main.screenPosition;

            // Center everything.
            headDrawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);

            // Floor the result to remove subpixel jittering.
            headDrawPosition += drawPlayer.headPosition + drawInfo.headVect;
            headDrawPosition += new Vector2(drawPlayer.direction * -4f, -6f).RotatedBy(drawPlayer.headRotation);
            headDrawPosition = new Vector2((int)headDrawPosition.X, (int)headDrawPosition.Y);

            drawInfo.DrawDataCache.Add(new(texture, headDrawPosition, null, drawInfo.colorArmorHead, drawPlayer.headRotation, texture.Size() * 0.5f, 1f, drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0));
        }
    }
}