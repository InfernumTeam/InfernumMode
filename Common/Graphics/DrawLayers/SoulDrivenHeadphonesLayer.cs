using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

            // It is imperative to use drawInfo.Position and not drawInfo.Player.Position, or else the layer will break on the player select & map (in the case of a head layer).
            Vector2 headDrawPosition = drawInfo.Position - Main.screenPosition;

            // Using drawPlayer to get width & height and such is perfectly fine, on the other hand. Just center everything.
            headDrawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);

            // Convert to int to remove the jitter.
            headDrawPosition += drawPlayer.headPosition + drawInfo.headVect;
            headDrawPosition += new Vector2(drawPlayer.direction * -4f, -6f);
            headDrawPosition = new Vector2((int)headDrawPosition.X, (int)headDrawPosition.Y);

            drawInfo.DrawDataCache.Add(new(texture, headDrawPosition, null, drawInfo.colorArmorHead, 0f, texture.Size() * 0.5f, 1f, drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0));
        }
    }
}