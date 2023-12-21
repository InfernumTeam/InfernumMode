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
        public override int ParallaxDepth => 24;

        public override Color MapColor => Color.Brown;

        public override Vector2 AdditionalOffset(int i, int j) => WorldSaveSystem.ProvidenceArena.TopLeft() - new Vector2(i + 1900, j + 1250);
    }
}
