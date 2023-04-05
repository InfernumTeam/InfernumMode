using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DoG
{
    public class DoGMusicSceneInfernum : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => BossRushEvent.BossRushActive ? SceneEffectPriority.None : (SceneEffectPriority)10;

        public override int NPCType => ModContent.NPCType<ProvidenceBoss>();

        public override int? MusicModMusic
        {
            get
            {
                int? phase1Music = Calamity.Instance.GetMusicFromMusicMod("DevourerofGodsPhase1");
                int? phase2Music = Calamity.Instance.GetMusicFromMusicMod("DevourerofGodsPhase2");
                if (CalamityGlobalNPC.DoGHead == -1 || !InfernumMode.CanUseCustomAIs)
                    return phase1Music;

                return DoGPhase1HeadBehaviorOverride.CurrentPhase2TransitionState != DoGPhase1HeadBehaviorOverride.Phase2TransitionState.NotEnteringPhase2 ? phase2Music : phase1Music;
            }
        }

        public override int VanillaMusic
        {
            get
            {
                int phase1Music = MusicID.Boss3;
                int phase2Music = MusicID.LunarBoss;
                if (CalamityGlobalNPC.DoGHead == -1 || !InfernumMode.CanUseCustomAIs)
                    return phase1Music;

                return DoGPhase1HeadBehaviorOverride.CurrentPhase2TransitionState != DoGPhase1HeadBehaviorOverride.Phase2TransitionState.NotEnteringPhase2 ? phase2Music : phase1Music;
            }
        }

        public override int OtherworldMusic
        {
            get
            {
                int phase1Music = MusicID.OtherworldlyBoss1;
                int phase2Music = MusicID.OtherworldlyLunarBoss;
                if (CalamityGlobalNPC.DoGHead == -1 || !InfernumMode.CanUseCustomAIs)
                    return phase1Music;

                return DoGPhase1HeadBehaviorOverride.CurrentPhase2TransitionState != DoGPhase1HeadBehaviorOverride.Phase2TransitionState.NotEnteringPhase2 ? phase2Music : phase1Music;
            }
        }
    }
}
