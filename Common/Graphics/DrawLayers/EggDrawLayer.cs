using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.DrawLayers
{
    public class EggDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.Infernum().GetValue<bool>("EggShieldActive") || drawInfo.drawPlayer.Infernum().GetValue<float>("EggShieldOpacity") > 0f)
                return drawInfo.shadow == 0;
            return false;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/DrawLayers/EggDrawLayer").Value;
            float centerX = drawInfo.Center.X;
            float centerY = drawInfo.Center.Y - 7f;
            Rectangle frame = new(0, 56 * drawInfo.drawPlayer.Infernum().GetValue<int>("CurrentEggShieldHits"), texture.Width, 56);

            drawInfo.DrawDataCache.Add(new DrawData(texture, new Vector2(centerX, centerY) - Main.screenPosition, frame, drawInfo.colorArmorBody * drawInfo.drawPlayer.Infernum().GetValue<float>("EggShieldOpacity"), 0f, frame.Size() * 0.5f, 1.3f, SpriteEffects.None, 0));
        }
    }
}
