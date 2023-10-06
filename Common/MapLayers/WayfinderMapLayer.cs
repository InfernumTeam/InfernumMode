using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Common.MapLayers
{
    public class WayfinderMapLayer : ModMapLayer
    {
        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (WorldSaveSystem.WayfinderGateLocation == Vector2.Zero)
                return;

            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/UI/WayfinderGateMap").Value;
            if (context.Draw(texture, WorldSaveSystem.WayfinderGateLocation / 16, Alignment.Bottom).IsMouseOver)
                text = "Wayfinder Gate";
        }
    }
}
