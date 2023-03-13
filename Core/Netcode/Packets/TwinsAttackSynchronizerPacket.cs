using System.IO;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Twins.TwinsAttackSynchronizer;

namespace InfernumMode.Core.Netcode.Packets
{
    public class TwinsAttackSynchronizerPacket : BaseInfernumPacket
    {
        // This packet should only ever be sent from the server to begin with.
        public override bool ResendFromServer => false;

        public override void Write(ModPacket packet, params object[] context)
        {
            packet.Write(_targetIndex);
            packet.Write(UniversalStateIndex);
            packet.Write(UniversalAttackTimer);
            packet.Write((int)CurrentAttackState);
        }

        public override void Read(BinaryReader reader)
        {
            _targetIndex = reader.ReadInt32();
            UniversalStateIndex = reader.ReadInt32();
            UniversalAttackTimer = reader.ReadInt32();
            CurrentAttackState = (TwinsAttackState)reader.ReadInt32();
        }
    }
}