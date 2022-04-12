using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class WorldSyncingSystem : ModSystem
    {
        public override void NetSend(BinaryWriter writer)
        {
            BitsByte flags = new();
            flags[0] = PoDWorld.InfernumMode;
            writer.Write(flags);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            PoDWorld.InfernumMode = flags[0];
        }
    }
}