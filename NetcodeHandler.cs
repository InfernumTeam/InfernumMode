using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public enum InfernumPacketType : short
    {
        SendExtraNPCData,
        SyncInfernumActive
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
                return true;

            // If the NPC is not active, it is possible that the packet which initialized the NPC has not been sent yet.
            // If so, wait until that happens.
            if (!Main.npc[NPCIndex].active)
                return false;

            if (CachedRealLife >= 0)
                Main.npc[NPCIndex].realLife = CachedRealLife;
            for (int i = 0; i < TotalUniqueIndicesUsed; i++)
                Main.npc[NPCIndex].Infernum().ExtraAI[ExtraAIIndicesUsed[i]] = ExtraAIValues[i];
            if (ArenaRectangle != default)
                Main.npc[NPCIndex].Infernum().Arena = ArenaRectangle;

            return true;
        }
    }

    public static class NetcodeHandler
    {
        internal static List<InfernumNPCSyncInformation> PendingSyncs = new();

        public static void SyncInfernumActivity(int sender)
        {
            // Don't bother trying to send packets in singleplayer.
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = InfernumMode.Instance.GetPacket();
            BitsByte containmentFlagWrapper = new();
            containmentFlagWrapper[0] = WorldSaveSystem.InfernumMode;

            packet.Write((short)InfernumPacketType.SyncInfernumActive);
            packet.Write(sender);
            packet.Write(containmentFlagWrapper);
            packet.Send(-1, sender);
        }

        public static void RecieveInfernumActivitySync(BinaryReader reader)
        {
            int sender = reader.ReadInt32();
            BitsByte flag = reader.ReadByte();
            WorldSaveSystem.InfernumMode = flag[0];

            // Send the packet again to the other clients if this packet was received on the server.
            // Since ModPackets go solely to the server when sent by a client this is necesssary
            // to ensure that all clients are informed of what happened.
            if (Main.netMode == NetmodeID.Server)
                SyncInfernumActivity(sender);
        }

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
                    Rectangle arenaRectangle = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

                    for (int i = 0; i < totalUniqueAIIndicesUsed; i++)
                    {
                        indicesUsed[i] = reader.ReadInt32();
                        aiValues[i] = reader.ReadSingle();
                    }
                    InfernumNPCSyncInformation syncInformation = new()
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

                case InfernumPacketType.SyncInfernumActive:
                    // Send the packet again to the other clients if this packet was received on the server.
                    // Since ModPackets go solely to the server when sent by a client this is necesssary
                    // to ensure that all clients are informed of what happened.
                    RecieveInfernumActivitySync(reader);
                    break;
            }
        }

        public static void Update()
        {
            PendingSyncs.RemoveAll(s => s.TryToApplyToNPC());
        }
    }
}