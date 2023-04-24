using CalamityMod.Events;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaverSceneInfernum : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => BossRushEvent.BossRushActive ? SceneEffectPriority.BossLow : (SceneEffectPriority)9;

        public override int NPCType => ModContent.NPCType<StormWeaverHead>();

        public override int? MusicModMusic
        {
            get
            {
                int weaverIndex = NPC.FindFirstNPC(ModContent.NPCType<StormWeaverHead>());
                int? defaultSWMusic = Calamity.Instance.GetMusicFromMusicMod("StormWeaver");
                if (weaverIndex == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultSWMusic;

                return defaultSWMusic;
            }
        }

        public override int VanillaMusic
        {
            get
            {
                int weaverIndex = NPC.FindFirstNPC(ModContent.NPCType<StormWeaverHead>());
                if (weaverIndex == -1 || !InfernumMode.CanUseCustomAIs)
                    return MusicID.Boss3;

                return MusicID.Boss2;
            }
        }

        public override int OtherworldMusic => VanillaMusic;
    }
}
