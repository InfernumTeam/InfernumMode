using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Core.Netcode.PacketManager;

namespace InfernumMode.Core.Netcode.Packets
{
    public class ExtraNPCDataPacket : BaseInfernumPacket
    {
        // General NPC sync packets (which should be the only source of these packets) only are fired from the server, and the context is hard to get
        // back once it's used on the server.
        public override bool ResendFromServer => false;

        public override void Write(ModPacket packet, params object[] context)
        {
            // Don't send anything if the NPC is invalid.
            if (context.Length <= 0 || context[0] is not NPC npc || !npc.active)
                return;

            int totalSlotsInUse = npc.Infernum().TotalAISlotsInUse;
            packet.Write(npc.whoAmI);
            packet.Write(npc.realLife);
            packet.Write(totalSlotsInUse);
            packet.Write(npc.Infernum().TotalPlayersAtStart ?? 1);
            packet.Write(npc.Infernum().Arena.X);
            packet.Write(npc.Infernum().Arena.Y);
            packet.Write(npc.Infernum().Arena.Width);
            packet.Write(npc.Infernum().Arena.Height);

            for (int i = 0; i < npc.Infernum().ExtraAI.Length; i++)
            {
                if (!npc.Infernum().HasAssociatedAIBeenUsed[i])
                    continue;

                packet.Write(i);
                packet.Write(npc.Infernum().ExtraAI[i]);
            }

            if (InfernumMode.CanUseCustomAIs)
                npc.BehaviorOverride<NPCBehaviorOverride>()?.SendExtraData(npc, packet);
        }

        public override void Read(BinaryReader reader)
        {
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
                PendingNPCSyncs.Add(syncInformation);

            if (InfernumMode.CanUseCustomAIs)
                Main.npc[npcIndex].BehaviorOverride<NPCBehaviorOverride>()?.ReceiveExtraData(Main.npc[npcIndex], reader);
        }
    }
}