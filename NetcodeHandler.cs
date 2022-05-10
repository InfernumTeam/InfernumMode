using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode
{
    public enum InfernumPacketType : short
    {
        SendExtraNPCData
    }

    public class InfernumNPCSyncInformation
    {
        public int NPCIndex = -1;
        public int CachedRealLife = -1;
        public int TotalUniqueIndicesUsed;
        public int[] ExtraAIIndicesUsed;
        public float[] ExtraAIValues;
        public Rectangle ArenaRectangle;

        public bool TryToApplyToNPC()
        {
            if (NPCIndex < 0)
                return false;

            // If the NPC is not active, it is possible that the packet which initialized the NPC has not been sent yet.
            // If so, wait until that happens.
            if (!Main.npc[NPCIndex].active)
                return false;

            if (CachedRealLife >= 0)
                Main.npc[NPCIndex].realLife = CachedRealLife;
            for (int i = 0; i < TotalUniqueIndicesUsed; i++)
                Main.npc[NPCIndex].Infernum().ExtraAI[ExtraAIIndicesUsed[i]] = ExtraAIValues[i];
            if (ArenaRectangle != default)
                Main.npc[NPCIndex].Infernum().arenaRectangle = ArenaRectangle;

            return true;
        }
    }

    public static class NetcodeHandler
    {
        public static List<InfernumNPCSyncInformation> PendingSyncs = new List<InfernumNPCSyncInformation>();
        public static void ReceivePacket(Mod mod, BinaryReader reader, int whoAmI)
        {
            InfernumPacketType packetType = (InfernumPacketType)reader.ReadInt16();
            switch (packetType)
            {
                case InfernumPacketType.SendExtraNPCData:
                    int npcIndex = reader.ReadInt32();
                    int realLife = reader.ReadInt32();
                    int totalUniqueAIIndicesUsed = reader.ReadInt32();
                    int[] indicesUsed = new int[totalUniqueAIIndicesUsed];
                    float[] aiValues = new float[totalUniqueAIIndicesUsed];
                    Rectangle arenaRectangle = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

                    for (int i = 0; i < totalUniqueAIIndicesUsed; i++)
                    {
                        indicesUsed[i] = reader.ReadInt32();
                        aiValues[i] = reader.ReadSingle();
                    }
                    InfernumNPCSyncInformation syncInformation = new InfernumNPCSyncInformation()
                    {
                        NPCIndex = npcIndex,
                        CachedRealLife = realLife,
                        TotalUniqueIndicesUsed = totalUniqueAIIndicesUsed,
                        ExtraAIIndicesUsed = indicesUsed,
                        ExtraAIValues = aiValues,
                        ArenaRectangle = arenaRectangle
                    };

                    if (!syncInformation.TryToApplyToNPC())
                        PendingSyncs.Add(syncInformation);

                    break;
            }
        }

        public static void Update()
        {
            PendingSyncs.RemoveAll(s => s.TryToApplyToNPC());
        }
    }
}