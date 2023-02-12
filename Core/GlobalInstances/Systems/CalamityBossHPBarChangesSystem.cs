using CalamityMod.NPCs.SlimeGod;
using Terraria.ModLoader;
using static CalamityMod.UI.BossHealthBarManager;
using static Terraria.ModLoader.ModContent;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class CalamityBossHPBarChangesSystem : ModSystem
    {
        public static void PerformBarChanges()
        {
            OneToMany.Remove(NPCType<EbonianSlimeGod>());
            OneToMany.Remove(NPCType<CrimulanSlimeGod>());
            BossExclusionList.Remove(NPCType<SlimeGodCore>());
        }

        public static void UndoBarChanges()
        {
            int[] slimeGods = new int[] { NPCType<EbonianSlimeGod>(), NPCType<SplitEbonianSlimeGod>(), NPCType<CrimulanSlimeGod>(), NPCType<SplitCrimulanSlimeGod>() };
            OneToMany[NPCType<EbonianSlimeGod>()] = slimeGods;
            OneToMany[NPCType<CrimulanSlimeGod>()] = slimeGods;

            if (BossExclusionList.Contains(NPCType<SlimeGodCore>()))
                BossExclusionList.Add(NPCType<SlimeGodCore>());
        }
    }
}