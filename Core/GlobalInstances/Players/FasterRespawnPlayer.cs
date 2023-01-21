using CalamityMod.CalPlayer;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class FasterRespawnPlayer : ModPlayer
    {
        public override void UpdateDead()
        {
            if (WorldSaveSystem.InfernumMode && !CalamityPlayer.areThereAnyDamnBosses)
                Player.respawnTimer = Utils.Clamp(Player.respawnTimer - 1, 0, 3600);
        }
    }
}