using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Core.Netcode
{
    public class InfernumNPCSyncInformation
    {
        public int NPCIndex = -1;
        public int CachedRealLife = -1;
        public int TotalUniqueIndicesUsed;
        public int TotalPlayersAtStart;
        public int[] ExtraAIIndicesUsed;
        public float[] ExtraAIValues;
        public Rectangle ArenaRectangle;

        public bool TryToApplyToNPC()
        {
            if (NPCIndex < 0)
                return true;

            // If the NPC is not active, it is possible that the packet which initialized the NPC has not been sent yet.
            // If so, wait until that happens.
            if (!Main.npc[NPCIndex].active)
                return false;

            if (CachedRealLife >= 0)
                Main.npc[NPCIndex].realLife = CachedRealLife;
            for (int i = 0; i < TotalUniqueIndicesUsed; i++)
            {
                Main.npc[NPCIndex].Infernum().HasAssociatedAIBeenUsed[ExtraAIIndicesUsed[i]] = true;
                Main.npc[NPCIndex].Infernum().ExtraAI[ExtraAIIndicesUsed[i]] = ExtraAIValues[i];
            }
            if (ArenaRectangle != default && Main.npc[NPCIndex].Infernum().Arena == default)
                Main.npc[NPCIndex].Infernum().Arena = ArenaRectangle;

            Main.npc[NPCIndex].Infernum().TotalPlayersAtStart = TotalPlayersAtStart;

            return true;
        }
    }
}