using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Systems;
using InfernumMode.GlobalInstances;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class GuardiansMusicScene : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
        public override int NPCType => ModContent.NPCType<ProfanedGuardianCommander>();
        public override int? MusicModMusic => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/CommanderLoop");
        public override int VanillaMusic => -1;
        public override int OtherworldMusic => -1;

        public override bool AdditionalCheck()
        {
            return GlobalNPCOverrides.CommanderSolo != -1;
        }
    }
}
