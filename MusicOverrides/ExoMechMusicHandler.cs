using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.Skies;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
	public class ExoMechMusicHandler : ModSceneEffect
    {
        public override int Music
        {
            get
            {
                if (InfernumMode.DraedonThemeTimer > 0)
                {
                    InfernumMode.DraedonThemeTimer++;
                    if (InfernumMode.DraedonThemeTimer >= DraedonBehaviorOverride.PostBattleMusicLength)
                        InfernumMode.DraedonThemeTimer = 0f;
                    else
                        return MusicLoader.GetMusicSlot(Mod, "Sounds/Music/ExoMechBosses");
                }

                if (!ExoMechsSky.CanSkyBeActive)
                    return MusicLoader.GetMusicSlot(InfernumMode.CalamityMod, "Sounds/Music/DraedonAmbience");

                return MusicLoader.GetMusicSlot(Mod, "Sounds/Music/ExoMechBosses");
            }
        }

        public override bool IsSceneEffectActive(Player player) => (ExoMechsSky.CanSkyBeActive || InfernumMode.DraedonThemeTimer > 0 || NPC.AnyNPCs(ModContent.NPCType<Draedon>())) 
            && InfernumMode.CanUseCustomAIs && !BossRushEvent.BossRushActive;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    }
}
