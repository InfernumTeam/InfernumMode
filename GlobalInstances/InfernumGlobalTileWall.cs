using InfernumMode.Systems;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class InfernumGlobalWall : GlobalWall
    {
        public override bool CanExplode(int i, int j, int type)
        {
            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 16, 16)))
                return false;

            return base.CanExplode(i, j, type);
        }

        public override void KillWall(int i, int j, int type, ref bool fail)
        {
            if (WorldSaveSystem.ProvidenceArena.Intersects(new(i, j, 16, 16)))
                fail = true;
        }
    }
}
