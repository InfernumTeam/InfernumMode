using CalamityMod;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.ILEditingStuff;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode
{
    public static class PacketHandler
    {
        internal static List<InfernumNPCSyncInformation> PendingSyncs = new();

        #region Send Methods
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

        public static void OpenLostColosseumPortalSync(int sender)
        {
            // Don't bother trying to send packets in singleplayer.
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = InfernumMode.Instance.GetPacket();
            BitsByte containmentFlagWrapper = new();
            containmentFlagWrapper[0] = WorldSaveSystem.HasOpenedLostColosseumPortal;

            packet.Write((short)InfernumPacketType.SyncInfernumActive);
            packet.Write(sender);
            packet.Write(containmentFlagWrapper);
            packet.Write(WorldSaveSystem.LostColosseumPortalAnimationTimer);
            packet.Send(-1, sender);
        }
        #endregion Send Methods

        #region Receive Methods
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

        public static void RecieveColosseumPortalOpeningSync(BinaryReader reader)
        {
            int sender = reader.ReadInt32();
            BitsByte flag = reader.ReadByte();
            WorldSaveSystem.LostColosseumPortalAnimationTimer = reader.ReadInt32();
            WorldSaveSystem.HasOpenedLostColosseumPortal = flag[0];

            // Send the packet again to the other clients if this packet was received on the server.
            // Since ModPackets go solely to the server when sent by a client this is necesssary
            // to ensure that all clients are informed of what happened.
            if (Main.netMode == NetmodeID.Server)
                OpenLostColosseumPortalSync(sender);
        }
        #endregion Receive Methods

        public static void SyncExoMechSummon(Player p)
        {
            // Don't bother trying to send packets in singleplayer.
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = InfernumMode.Instance.GetPacket();
            packet.Write((short)InfernumPacketType.SummonExoMech);
            packet.Write((short)p.whoAmI);
            packet.Write((int)(DrawDraedonSelectionUIWithAthena.PrimaryMechToSummon ?? 0));
            packet.Write((int)(DrawDraedonSelectionUIWithAthena.DestroyerTypeToSummon ?? 0));
            packet.Send();
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
                    int totalPlayersAtStart = reader.ReadInt32();
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
                        TotalPlayersAtStart = totalPlayersAtStart,
                        ExtraAIIndicesUsed = indicesUsed,
                        ExtraAIValues = aiValues,
                        ArenaRectangle = arenaRectangle
                    };

                    if (!syncInformation.TryToApplyToNPC())
                        PendingSyncs.Add(syncInformation);
                    else if (InfernumMode.CanUseCustomAIs)
                    {
                        var behaviorOverride = Main.npc[npcIndex].BehaviorOverride<NPCBehaviorOverride>();

                        // If the behavior override is not registered for some reason, ensure that there aren't any leftover bytes to read by the end.
                        if (behaviorOverride is null && Main.npc[npcIndex].active)
                        {
                            long remainingBytesToRead = reader.BaseStream.Length - reader.BaseStream.Position;
                            reader.ReadBytes((int)remainingBytesToRead);
                        }

                        // Otherwise, read the data as dictatedby the behavior override.
                        else
                            behaviorOverride.ReceiveExtraData(Main.npc[npcIndex], reader);
                    }
                    break;

                case InfernumPacketType.SyncInfernumActive:
                    RecieveInfernumActivitySync(reader);
                    break;
                case InfernumPacketType.SummonExoMech:
                    Player player = Main.player[reader.ReadInt16()];
                    DrawDraedonSelectionUIWithAthena.PrimaryMechToSummon = (ExoMech)reader.ReadInt32();
                    DrawDraedonSelectionUIWithAthena.DestroyerTypeToSummon = (ExoMech)reader.ReadInt32();
                    DraedonBehaviorOverride.SummonExoMech(player);
                    break;
                case InfernumPacketType.UpdateTwinsAttackSynchronizer:
                    TwinsAttackSynchronizer.ReadFromPacket(reader);
                    break;
                case InfernumPacketType.OpenLostColosseumPortal:
                    RecieveColosseumPortalOpeningSync(reader);
                    break;
            }
        }

        public static void Update()
        {
            PendingSyncs.RemoveAll(s => s.TryToApplyToNPC());
        }
    }
}