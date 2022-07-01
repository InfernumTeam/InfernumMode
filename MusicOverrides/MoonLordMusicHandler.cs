using InfernumMode.BehaviorOverrides.BossAIs.MoonLord;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.MusicOverrides
{
    public class MoonLordMusicHandler : ModSceneEffect
    {
        public static NPC MoonLord
        {
            get
            {
                int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
                if (moonLordIndex != -1)
                    return Main.npc[moonLordIndex];
                return null;
            }
        }
        public override int Music => (MoonLord?.Infernum().ExtraAI[10] ?? 0f) >= MoonLordCoreBehaviorOverride.IntroSoundLength ?
            MusicLoader.GetMusicSlot(Mod, "Sounds/Music/MoonLord") : 0;

        public override bool IsSceneEffectActive(Player player) => MoonLord != null;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (!isActive)
                return;

            Main.musicFade[Main.curMusic] = 1f;
        }

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    }
}
