using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Walls
{
    public sealed class ProfanedTempleBGWall : BaseParallaxWall
    {
        public override int ParallaxDepth => 24;

        public override Color MapColor => new(28, 11, 10);

        public override Vector2 AdditionalOffset(int i, int j) => WorldSaveSystem.ProvidenceArena.TopLeft() - new Vector2(i + 1900, j + 1250);
    }
}
