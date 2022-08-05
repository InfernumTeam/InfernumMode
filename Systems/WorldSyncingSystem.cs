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
            flags[0] = WorldSaveSystem.InfernumMode;
            flags[1] = WorldSaveSystem.HasBeatedInfernumNightProvBeforeDay;
            flags[2] = WorldSaveSystem.HasBeatedInfernumProvRegularly;
            writer.Write(flags);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            WorldSaveSystem.InfernumMode = flags[0];
            WorldSaveSystem.HasBeatedInfernumNightProvBeforeDay = flags[1];
            WorldSaveSystem.HasBeatedInfernumProvRegularly = flags[2];
        }
    }
}