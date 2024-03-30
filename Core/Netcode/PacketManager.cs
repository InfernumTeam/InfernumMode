using InfernumMode.Core.Netcode.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace InfernumMode.Core.Netcode
{
    public class PacketManager : ModSystem
    {
        internal static List<InfernumNPCSyncInformation> PendingNPCSyncs = [];

        internal static Dictionary<string, BaseInfernumPacket> RegisteredPackets = [];

        public override void OnModLoad()
        {
            RegisteredPackets = [];
            foreach (Type t in AssemblyManager.GetLoadableTypes(Mod.Code))
            {
                if (!t.IsSubclassOf(typeof(BaseInfernumPacket)) || t.IsAbstract)
                    continue;

                BaseInfernumPacket packet = Activator.CreateInstance(t) as BaseInfernumPacket;
                RegisteredPackets[t.FullName] = packet;
            }
        }

        internal static void PreparePacket(BaseInfernumPacket packet, object[] context, short? sender = null)
        {
            // Don't try to send packets in singleplayer.
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            // Assume the sender is the current client if nothing else is supplied.
            sender ??= (short)Main.myPlayer;

            ModPacket wrapperPacket = InfernumMode.Instance.GetPacket();

            // Write the identification header. This is necessary to ensure that on the receiving end the reader know how to interpret the packet.
            wrapperPacket.Write(packet.GetType().FullName);

            // Write the sender if the packet needs to be re-sent from the server.
            if (packet.ResendFromServer)
                wrapperPacket.Write(sender.Value);

            // Write the requested packet data.
            packet.Write(wrapperPacket, context);

            // Send the packet.
            wrapperPacket.Send(-1, sender.Value);
        }

        public static void SendPacket<T>(params object[] context) where T : BaseInfernumPacket
        {
            // Verify that the packet is registered before trying to send it.
            string packetName = typeof(T).FullName;
            if (Main.netMode == NetmodeID.SinglePlayer || !RegisteredPackets.TryGetValue(packetName, out BaseInfernumPacket packet))
                return;

            PreparePacket(packet, context);
        }

        public static void ReceivePacket(BinaryReader reader)
        {
            // Read the identification header to determine how the packet should be processed.
            string packetName = reader.ReadString();

            // If no valid packet could be found, get out of here.
            // There will inevitably be a reader underflow error caused by TML's packet policing, but there aren't any clear-cut solutions that
            // I know of that adequately addresses that problem, and from what I can tell it's never catastrophic when it happens.
            if (!RegisteredPackets.TryGetValue(packetName, out BaseInfernumPacket packet))
                return;

            // Determine who sent this packet if it needs to resend.
            short sender = -1;
            if (packet.ResendFromServer)
                sender = reader.ReadInt16();

            // Read off requested packet data.
            packet.Read(reader);

            // If this packet was received server-side and the packet needs to be re-sent, send it back to all the clients, with the
            // exception of the one that originally brought this packet to the server.

            // TODO -- Through this the original context is destroyed. How would this be best addressed? By forcing packets to glue the context back together
            // in the Read hook? That seems a bit stupid, but I don't know what options there are that are actually reasonable.
            if (Main.netMode == NetmodeID.Server && packet.ResendFromServer)
                PreparePacket(packet, Array.Empty<object>(), sender);
        }

        public override void PostUpdateNPCs()
        {
            PendingNPCSyncs.RemoveAll(s => s.ShouldBeDiscarded || s.TryToApplyToNPC());
        }
    }
}