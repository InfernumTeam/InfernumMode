using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class EyeOfCthulhuMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/EyeOfCthulhu");

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(NPCID.EyeofCthulhu);

        public override SceneEffectPriority Priority => SceneEffectPriority.BossLow;
    }
}
