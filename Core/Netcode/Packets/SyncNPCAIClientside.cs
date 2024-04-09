using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode.Packets
{
    public class SyncNPCAIClientside : BaseInfernumPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            NPC npc = Main.npc[(int)context[0]];
            packet.Write((int)context[0]);
            for (int i = 0; i < NPC.maxAI; i++)
                packet.Write(npc.ai[i]);

            new ExtraNPCDataPacket().Write(packet, new[] { Main.npc[(int)context[0]] });
        }

        public override void Read(BinaryReader reader)
        {
            NPC npc = Main.npc[reader.ReadInt32()];
            for (int i = 0; i < NPC.maxAI; i++)
                npc.ai[i] = reader.ReadSingle();

            new ExtraNPCDataPacket().Read(reader);
            npc.netUpdate = true;
        }
    }
}
