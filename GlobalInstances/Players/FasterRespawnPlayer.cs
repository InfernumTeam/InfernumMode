using InfernumMode.Systems;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.Players
{
    public class FasterRespawnPlayer : ModPlayer
    {
        public override void UpdateDead()
        {
            if (WorldSaveSystem.InfernumMode)
                Player.respawnTimer = Utils.Clamp(Player.respawnTimer - 1, 0, 3600);
        }
    }
}