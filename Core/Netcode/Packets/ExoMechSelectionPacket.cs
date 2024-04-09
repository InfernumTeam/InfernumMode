using System.IO;
using CalamityMod;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode.Packets
{
    public class ExoMechSelectionPacket : BaseInfernumPacket
    {
        // Once the SummonExoMech method is called server-side and the NPC spawns are dispatched, this packet's job is done.
        public override bool ResendFromServer => false;

        public override void Write(ModPacket packet, params object[] context)
        {
            packet.Write((short)Main.myPlayer);
            packet.Write((int)(CustomExoMechSelectionSystem.PrimaryMechToSummon ?? 0));
            packet.Write((int)(CustomExoMechSelectionSystem.DestroyerTypeToSummon ?? 0));
        }

        public override void Read(BinaryReader reader)
        {
            Player player = Main.player[reader.ReadInt16()];
            CustomExoMechSelectionSystem.PrimaryMechToSummon = (ExoMech)reader.ReadInt32();
            CustomExoMechSelectionSystem.DestroyerTypeToSummon = (ExoMech)reader.ReadInt32();
            DraedonBehaviorOverride.SummonExoMech(player);
        }
    }
}