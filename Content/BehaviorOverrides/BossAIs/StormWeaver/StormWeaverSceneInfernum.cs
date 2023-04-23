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
                int? defaultCVMusic = Calamity.Instance.GetMusicFromMusicMod("StormWeaver");
                if (weaverIndex == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultCVMusic;

                return Main.npc[weaverIndex].ai[1] != 0f ? defaultCVMusic : MusicID.SpaceDay;
            }
        }

        public override int VanillaMusic
        {
            get
            {
                int weaverIndex = NPC.FindFirstNPC(ModContent.NPCType<StormWeaverHead>());
                if (weaverIndex == -1 || !InfernumMode.CanUseCustomAIs)
                    return MusicID.Boss3;

                return Main.npc[weaverIndex].ai[1] != 0f ? MusicID.Boss2 : MusicID.SpaceDay;
            }
        }

        public override int OtherworldMusic => VanillaMusic;
    }
}
