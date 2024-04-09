using System.IO;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode.Packets
{
    public class InfernumModeActivityPacket : BaseInfernumPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            BitsByte containmentFlagWrapper = new()
            {
                [0] = WorldSaveSystem.InfernumModeEnabled
            };
            packet.Write(containmentFlagWrapper);
        }

        public override void Read(BinaryReader reader)
        {
            BitsByte containmentFlagWrapper = reader.ReadByte();
            WorldSaveSystem.InfernumModeEnabled = containmentFlagWrapper[0];
        }
    }
}