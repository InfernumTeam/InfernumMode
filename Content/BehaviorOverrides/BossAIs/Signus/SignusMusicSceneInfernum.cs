using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;
using SignusBoss = CalamityMod.NPCs.Signus.Signus;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class SignusMusicSceneInfernum : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => BossRushEvent.BossRushActive ? SceneEffectPriority.None : (SceneEffectPriority)9;

        public override int NPCType => ModContent.NPCType<SignusBoss>();

        public override int? MusicModMusic
        {
            get
            {
                int? defaultSignusMusic = Calamity.Instance.GetMusicFromMusicMod("Signus");
                int? ambienceMusic = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Assets/Sounds/Music/SignusAmbience");
                if (CalamityGlobalNPC.signus == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultSignusMusic;

                if (Main.npc[CalamityGlobalNPC.signus].ai[1] == 0f && !Main.npc[CalamityGlobalNPC.signus].WithinRange(Main.LocalPlayer.Center, 1200f))
                    return MusicID.Hell;

                return Main.npc[CalamityGlobalNPC.signus].ai[1] != 0f ? defaultSignusMusic : ambienceMusic;
            }
        }

        public override int VanillaMusic
        {
            get
            {
                int ambienceMusic = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Assets/Sounds/Music/SignusAmbience");
                if (CalamityGlobalNPC.signus == -1 || !InfernumMode.CanUseCustomAIs)
                    return MusicID.Boss4;

                if (Main.npc[CalamityGlobalNPC.signus].ai[1] == 0f && !Main.npc[CalamityGlobalNPC.signus].WithinRange(Main.LocalPlayer.Center, 1200f))
                    return MusicID.Hell;

                return Main.npc[CalamityGlobalNPC.signus].ai[1] != 0f ? MusicID.Boss4 : ambienceMusic;
            }
        }

        public override int OtherworldMusic => VanillaMusic;
    }
}
