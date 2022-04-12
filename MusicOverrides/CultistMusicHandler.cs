using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class CultistMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/LunaticCultist");

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(NPCID.CultistBoss);

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;
    }
}
