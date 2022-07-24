using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class EmpressMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/EmpressOfLight");

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(NPCID.HallowBoss);

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    }
}
