using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Miscellaneous
{
    public class WayfinderMapLayer : IMapLayer
    {
        public bool Visible
        {
            get;
            set;
        } = true;

        public void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (WorldSaveSystem.WayfinderGateLocation == Vector2.Zero)
                return;

            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/WayfinderGateMap").Value;
            if(context.Draw(texture, WorldSaveSystem.WayfinderGateLocation / 16, Alignment.Bottom).IsMouseOver)
                text = "Wayfinder Gate";
        }
    }
}