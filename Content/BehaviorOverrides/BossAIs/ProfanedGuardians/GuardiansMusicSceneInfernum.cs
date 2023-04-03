using CalamityMod.Events;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Systems;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;


namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class GuardiansMusicSceneInfernum : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => BossRushEvent.BossRushActive ? SceneEffectPriority.None : (SceneEffectPriority)9;

        public override int NPCType => ModContent.NPCType<ProfanedGuardianCommander>();

        public override int? MusicModMusic => Calamity.Instance.GetMusicFromMusicMod("ProfanedGuardians");

        public override int MusicDistance => 7000;

        public override int VanillaMusic => MusicID.Boss1;

        public override int OtherworldMusic => MusicID.OtherworldlyBoss1;        
    }
}
