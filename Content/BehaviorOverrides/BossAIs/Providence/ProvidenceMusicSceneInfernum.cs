using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceMusicSceneInfernum : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => BossRushEvent.BossRushActive ? SceneEffectPriority.None : (SceneEffectPriority)25;

        public override int NPCType => ModContent.NPCType<ProvidenceBoss>();

        public static bool ProvidenceIsInPhase2
        {
            get
            {
                if (CalamityGlobalNPC.holyBoss == -1)
                    return false;

                return Main.npc[CalamityGlobalNPC.holyBoss].life / (float)Main.npc[CalamityGlobalNPC.holyBoss].lifeMax < ProvidenceBehaviorOverride.Phase2LifeRatio;
            }
        }

        public override int? MusicModMusic
        {
            get
            {
                int? defaultProviMusic = Calamity.Instance.GetMusicFromMusicMod("Providence");
                int? guardiansMusic = Calamity.Instance.GetMusicFromMusicMod("ProfanedGuardians");
                if (CalamityGlobalNPC.holyBoss == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultProviMusic;

                if (Main.npc[CalamityGlobalNPC.holyBoss].ai[0] == (float)ProvidenceBehaviorOverride.ProvidenceAttackType.CrystalForm)
                    return 0;

                return ProvidenceIsInPhase2 ? defaultProviMusic : guardiansMusic;
            }
        }

        public override int VanillaMusic
        {
            get
            {
                int defaultProviMusic = MusicID.LunarBoss;
                int guardiansMusic = MusicID.Boss1;
                if (CalamityGlobalNPC.holyBoss == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultProviMusic;

                if (Main.npc[CalamityGlobalNPC.holyBoss].ai[0] == (float)ProvidenceBehaviorOverride.ProvidenceAttackType.CrystalForm)
                    return 0;

                return ProvidenceIsInPhase2 ? defaultProviMusic : guardiansMusic;
            }
        }

        public override int OtherworldMusic
        {
            get
            {
                int defaultProviMusic = MusicID.OtherworldlyLunarBoss;
                int guardiansMusic = MusicID.OtherworldlyBoss1;
                if (CalamityGlobalNPC.holyBoss == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultProviMusic;

                if (Main.npc[CalamityGlobalNPC.holyBoss].ai[0] == (float)ProvidenceBehaviorOverride.ProvidenceAttackType.CrystalForm)
                    return 0;

                return ProvidenceIsInPhase2 ? defaultProviMusic : guardiansMusic;
            }
        }
    }
}
