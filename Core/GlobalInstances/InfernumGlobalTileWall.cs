using InfernumMode.Content.Subworlds;
using InfernumMode.Core.GlobalInstances.Systems;
using SubworldLibrary;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances
{
    public class InfernumGlobalWall : GlobalWall
    {
        public override bool CanExplode(int i, int j, int type)
        {
            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 16, 16)) || SubworldSystem.IsActive<LostColosseum>())
                return false;

            return base.CanExplode(i, j, type);
        }

        public override bool CanPlace(int i, int j, int type)
        {
            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 1, 1)) || SubworldSystem.IsActive<LostColosseum>())
                return false;

            return base.CanPlace(i, j, type);
        }

        public override void KillWall(int i, int j, int type, ref bool fail)
        {
            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 16, 16)) || SubworldSystem.IsActive<LostColosseum>())
                fail = true;
        }
    }
}
