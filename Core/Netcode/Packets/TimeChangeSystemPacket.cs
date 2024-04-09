using System.IO;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode.Packets
{
    public class TimeChangeSystemPacket : BaseInfernumPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            BitsByte containmentFlagWrapper = new()
            {
                [0] = HyperplaneMatrixTimeChangeSystem.SeekingDayTime
            };
            packet.Write(containmentFlagWrapper);
            packet.Write(HyperplaneMatrixTimeChangeSystem.SoughtTime ?? -1);
        }

        public override void Read(BinaryReader reader)
        {
            BitsByte containmentFlagWrapper = reader.ReadByte();
            HyperplaneMatrixTimeChangeSystem.SeekingDayTime = containmentFlagWrapper[0];
            HyperplaneMatrixTimeChangeSystem.SoughtTime = reader.ReadInt32();

            if (HyperplaneMatrixTimeChangeSystem.SoughtTime <= -1)
                HyperplaneMatrixTimeChangeSystem.SoughtTime = null;
        }
    }
}