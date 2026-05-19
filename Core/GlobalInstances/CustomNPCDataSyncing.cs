using System.IO;
using System.Linq;
using CalamityMod.NPCs.CalClone;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using CalamityMod.NPCs.SupremeCalamitas;

namespace InfernumMode.Core.GlobalInstances
{
    public partial class GlobalNPCOverrides
    {
        internal bool[] HasAssociatedAIBeenUsed = new bool[TotalExtraAISlots];

        internal int TotalAISlotsInUse => HasAssociatedAIBeenUsed.Count(slot => slot);

        public override void PostAI(NPC npc)
        {
            for (int i = 0; i < ExtraAI.Length; i++)
            {
                if (ExtraAI[i] != 0f)
                    HasAssociatedAIBeenUsed[i] = true;
            }
        }

        /* Akira: I didn't know if there was a better place to put this, but felt like it made the most sense. 
         * This seems to fix a lot of jitter with the brothers, and MP patch claims the following lines below:
              Catastrophe sets its ai to refer to cataclysm's ai[], which causes problems when that NPC disappears and is replaced with another.
              In multiplayer, receiving info of a new NPC does not create a new array with the new ai values, but instead replaces the contents.
              Gradually, more and more NPCs share the same ai as the reference is never undone and you are henceforth cursed forever. */
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            if (npc.type == ModContent.NPCType<Cataclysm>() || npc.type == ModContent.NPCType<Catastrophe>() || npc.type == ModContent.NPCType<SupremeCataclysm>() || npc.type == ModContent.NPCType<SupremeCatastrophe>())
            {
                if (!npc.active)
                {
                    npc.ai = new float[NPC.maxAI];
                }
            }
        }
    }
}
