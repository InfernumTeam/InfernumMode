using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Walls
{
    public sealed class ProfanedTempleBGWall : BaseParallaxWall
    {
        public override int ParallaxDepth => 14;

        public override Color MapColor => Color.SaddleBrown;

        public override Vector2 AdditionalOffset(int i, int j) => WorldSaveSystem.ProvidenceArena.TopLeft() - new Vector2(i - 300, j - 1500);
    }
}
