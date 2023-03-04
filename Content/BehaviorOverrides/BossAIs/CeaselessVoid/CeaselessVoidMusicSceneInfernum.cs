using CalamityMod.NPCs;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;
using CVBoss = CalamityMod.NPCs.CeaselessVoid.CeaselessVoid;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVoidMusicSceneInfernum : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)9;

        public override int NPCType => ModContent.NPCType<CVBoss>();

        public override int? MusicModMusic
        {
            get
            {
                int? defaultCVMusic = Calamity.Instance.GetMusicFromMusicMod("CeaselessVoid");
                if (CalamityGlobalNPC.voidBoss == -1 || !InfernumMode.CanUseCustomAIs)
                    return defaultCVMusic;

                return Main.npc[CalamityGlobalNPC.voidBoss].ai[0] != 0f ? defaultCVMusic : MusicID.Dungeon;
            }
        }

        public override int VanillaMusic
        {
            get
            {
                if (CalamityGlobalNPC.voidBoss == -1 || !InfernumMode.CanUseCustomAIs)
                    return MusicID.Boss2;

                return Main.npc[CalamityGlobalNPC.voidBoss].ai[0] != 0f ? MusicID.Boss2 : MusicID.Dungeon;
            }
        }

        public override int OtherworldMusic => VanillaMusic;
    }
}
