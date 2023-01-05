using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Systems
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

                ModPacket packet = InfernumMode.Instance.GetPacket();

                int totalSlotsInUse = npc.Infernum().TotalAISlotsInUse;
                packet.Write((short)InfernumPacketType.SendExtraNPCData);
                packet.Write(npc.whoAmI);
                packet.Write(npc.realLife);
                packet.Write(totalSlotsInUse);
                packet.Write(npc.Infernum().TotalPlayersAtStart ?? 1);
                packet.Write(npc.Infernum().Arena.X);
                packet.Write(npc.Infernum().Arena.Y);
                packet.Write(npc.Infernum().Arena.Width);
                packet.Write(npc.Infernum().Arena.Height);
                
                for (int i = 0; i < npc.Infernum().ExtraAI.Length; i++)
                {
                    if (!npc.Infernum().HasAssociatedAIBeenUsed[i])
                        continue;

                    packet.Write(i);
                    packet.Write(npc.Infernum().ExtraAI[i]);
                }

                if (InfernumMode.CanUseCustomAIs)
                    npc.BehaviorOverride<NPCBehaviorOverride>()?.SendExtraData(npc, packet);

                packet.Send();

                // Have the twins send a specialized packet to ensure that the attack synchronizer is updated.
                if (npc.type is NPCID.Retinazer or NPCID.Spazmatism)
                    TwinsAttackSynchronizer.SyncState();
            }
            return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
    }
}