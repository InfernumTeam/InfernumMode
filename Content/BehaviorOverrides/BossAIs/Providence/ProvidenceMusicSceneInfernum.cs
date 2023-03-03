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
        public override SceneEffectPriority Priority => (SceneEffectPriority)9;

        public override int NPCType => ModContent.NPCType<ProvidenceBoss>();

        public override int? MusicModMusic
        {
            get
            {
                int? defaultProviMusic = Calamity.Instance.GetMusicFromMusicMod("Providence");
                int? guardiansMusic = Calamity.Instance.GetMusicFromMusicMod("ProfanedGuardians");
                if (CalamityGlobalNPC.holyBoss == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultProviMusic;
                
                return (Main.npc[CalamityGlobalNPC.holyBoss].life / (float)Main.npc[CalamityGlobalNPC.holyBoss].lifeMax < ProvidenceBehaviorOverride.Phase2LifeRatio) ? defaultProviMusic : guardiansMusic;
            }
        }

        public override int VanillaMusic => MusicID.LunarBoss;
        
        public override int OtherworldMusic => MusicID.OtherworldlyLunarBoss;
    }
}
