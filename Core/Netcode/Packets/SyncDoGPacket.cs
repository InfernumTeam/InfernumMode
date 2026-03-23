using System.IO;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.DoG.DoGPhase1HeadBehaviorOverride;

namespace InfernumMode.Core.Netcode.Packets
{
    public class SyncDoGPacket : BaseInfernumPacket
    {
        public override bool ResendFromServer => false;

        public override void Read(BinaryReader reader)
        {
            int npcIndex = reader.ReadInt32();
            UpdateDoGPhaseServer(npcIndex);
        }

        public override void Write(ModPacket packet, params object[] context)
        {
            packet.Write((int)context[0]);
        }
    }
}
