using InfernumMode.Core.GlobalInstances.Systems;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode.Packets
{
    public class ColosseumPortalOpenPacket : BaseInfernumPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            BitsByte containmentFlagWrapper = new()
            {
                [0] = WorldSaveSystem.HasOpenedLostColosseumPortal
            };
            packet.Write(containmentFlagWrapper);
            packet.Write(WorldSaveSystem.LostColosseumPortalAnimationTimer);
        }

        public override void Read(BinaryReader reader)
        {
            BitsByte containmentFlagWrapper = reader.ReadByte();
            WorldSaveSystem.HasOpenedLostColosseumPortal = containmentFlagWrapper[0];
            WorldSaveSystem.LostColosseumPortalAnimationTimer = reader.ReadInt32();
        }
    }
}