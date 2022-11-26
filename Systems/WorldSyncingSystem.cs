using System.IO;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Systems.WorldSaveSystem;

namespace InfernumMode.Systems
{
    public class WorldSyncingSystem : ModSystem
    {
        public override void NetSend(BinaryWriter writer)
        {
            BitsByte flags = new();
            flags[0] = WorldSaveSystem.InfernumMode;
            flags[1] = HasBeatedInfernumNightProvBeforeDay;
            flags[2] = HasBeatedInfernumProvRegularly;
            flags[3] = HasProvidenceDoorShattered;
            flags[4] = HasSepulcherAnimationBeenPlayed;
            flags[5] = InPostAEWUpdateWorld;
            flags[6] = HasDefeatedEidolists;

            writer.Write(flags);
            
            writer.Write(AbyssLayer1ForestSeed);
            writer.Write(AbyssLayer3CavernSeed);
            writer.Write(SquidDenCenter.X);
            writer.Write(SquidDenCenter.Y);
            writer.Write(EidolistWorshipPedestalCenter.X);
            writer.Write(EidolistWorshipPedestalCenter.Y);

            writer.Write(ProvidenceArena.X);
            writer.Write(ProvidenceArena.Y);
            writer.Write(ProvidenceArena.Width);
            writer.Write(ProvidenceArena.Height);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            WorldSaveSystem.InfernumMode = flags[0];
            HasBeatedInfernumNightProvBeforeDay = flags[1];
            HasBeatedInfernumProvRegularly = flags[2];
            HasProvidenceDoorShattered = flags[3];
            HasSepulcherAnimationBeenPlayed = flags[4];
            InPostAEWUpdateWorld = flags[5];
            HasDefeatedEidolists = flags[6];

            AbyssLayer1ForestSeed = reader.ReadInt32();
            AbyssLayer3CavernSeed = reader.ReadInt32();
            SquidDenCenter = new(reader.ReadInt32(), reader.ReadInt32());
            EidolistWorshipPedestalCenter = new(reader.ReadInt32(), reader.ReadInt32());

            ProvidenceArena = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
    }
}