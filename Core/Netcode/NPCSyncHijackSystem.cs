using InfernumMode.Core.Netcode.Packets;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Core.Netcode
{
    public class NPCSyncHijackSystem : ModSystem
    {
        public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            if (msgType == MessageID.SyncNPC)
            {
                NPC npc = Main.npc[number];
                if (!npc.active || !OverridingListManager.Registered(npc.type))
                    return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);

                // Sync extra general information about the NPC.
                PacketManager.SendPacket<ExtraNPCDataPacket>(Main.npc[number]);

                // Have the twins send a specialized packet to ensure that the attack synchronizer is updated.
                if (npc.type is NPCID.Retinazer or NPCID.Spazmatism)
                    PacketManager.SendPacket<TwinsAttackSynchronizerPacket>();
            }
            return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
    }
}
