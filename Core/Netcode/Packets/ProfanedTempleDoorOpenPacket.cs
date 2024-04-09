using System.IO;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode.Packets
{
    public class ProfanedTempleDoorOpenPacket : BaseInfernumPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            BitsByte containmentFlagWrapper = new()
            {
                [0] = WorldSaveSystem.HasProvidenceDoorShattered
            };
            packet.Write(containmentFlagWrapper);
        }

        public override void Read(BinaryReader reader)
        {
            BitsByte containmentFlagWrapper = reader.ReadByte();
            WorldSaveSystem.HasProvidenceDoorShattered = containmentFlagWrapper[0];
        }
    }
}