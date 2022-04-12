using CalamityMod.Events;
using CalamityMod.Skies;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class ExoMechMusicHandler : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/ExoMechBosses");

        public override bool IsSceneEffectActive(Player player) => ExoMechsSky.CanSkyBeActive && InfernumMode.CanUseCustomAIs && !BossRushEvent.BossRushActive;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    }
}
