using CalamityMod.Events;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class MechBossMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/MechBosses");

        public override bool IsSceneEffectActive(Player player) => 
            (NPC.AnyNPCs(NPCID.SkeletronPrime) || NPC.AnyNPCs(NPCID.Retinazer) || NPC.AnyNPCs(NPCID.Spazmatism) || NPC.AnyNPCs(NPCID.TheDestroyer)) && 
            InfernumMode.CanUseCustomAIs && !BossRushEvent.BossRushActive;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossLow;
    }
}
