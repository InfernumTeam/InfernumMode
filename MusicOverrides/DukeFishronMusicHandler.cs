using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class DukeFishronMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/DukeFishron");

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(NPCID.DukeFishron);

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;
    }
}
