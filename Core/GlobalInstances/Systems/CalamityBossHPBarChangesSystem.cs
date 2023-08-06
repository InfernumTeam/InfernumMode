using CalamityMod.NPCs.CeaselessVoid;
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
            OneToMany.Remove(NPCType<EbonianPaladin>());
            OneToMany.Remove(NPCType<CrimulanPaladin>());

            OneToMany.Remove(NPCType<CeaselessVoid>());
            OneToMany.Remove(NPCType<DarkEnergy>());

            BossExclusionList.Remove(NPCType<SlimeGodCore>());
            EntityExtensionHandler.Remove(NPCType<CeaselessVoid>());
        }

        public static void UndoBarChanges()
        {
            int[] slimeGods = new int[] { NPCType<EbonianPaladin>(), NPCType<SplitEbonianPaladin>(), NPCType<CrimulanPaladin>(), NPCType<SplitCrimulanPaladin>() };
            OneToMany[NPCType<EbonianPaladin>()] = slimeGods;
            OneToMany[NPCType<CrimulanPaladin>()] = slimeGods;

            int[] ceaselessVoid = new int[] { NPCType<CeaselessVoid>(), NPCType<DarkEnergy>() };
            OneToMany[NPCType<CeaselessVoid>()] = ceaselessVoid;
            OneToMany[NPCType<DarkEnergy>()] = ceaselessVoid;

            if (BossExclusionList.Contains(NPCType<SlimeGodCore>()))
                BossExclusionList.Add(NPCType<SlimeGodCore>());

            EntityExtensionHandler[NPCType<CeaselessVoid>()] = new BossEntityExtension(GetModNPC(NPCType<DarkEnergy>()).DisplayName, NPCType<DarkEnergy>());
        }
    }
}
