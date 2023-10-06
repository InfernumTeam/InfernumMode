using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge.AquaticScourgeHeadBehaviorOverride;

namespace InfernumMode.Core.Netcode.Packets
{
    public class CreateASWormSegmentsPacket : BaseInfernumPacket
    {
        // This packet should only ever be sent from the server to begin with.
        public override bool ResendFromServer => false;

        public override void Write(ModPacket packet, params object[] context)
        {
            packet.Write(WormSegments.Count);
            for (int i = 0; i < WormSegments.Count; i++)
            {
                packet.Write(WormSegments[i].Locked);
                packet.WritePackedVector2(WormSegments[i].Position);
                packet.WritePackedVector2(WormSegments[i].Velocity);
            }
        }

        public override void Read(BinaryReader reader)
        {
            WormSegments = new();
            int segmentCount = reader.ReadInt32();
            for (int i = 0; i < segmentCount; i++)
            {
                bool locked = reader.ReadBoolean();
                Vector2 position = reader.ReadPackedVector2();
                Vector2 velocity = reader.ReadPackedVector2();
                WormSegments.Add(new(position, velocity, locked));
            }
        }
    }
}