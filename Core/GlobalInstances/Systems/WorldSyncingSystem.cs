using System.IO;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Core.GlobalInstances.Systems.WorldSaveSystem;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldSyncingSystem : ModSystem
    {
        public override void NetSend(BinaryWriter writer)
        {
            BitsByte flags = new();
            BitsByte flags2 = new();
            flags[0] = InfernumModeEnabled;
            flags[1] = HasBeatenInfernumNightProvBeforeDay;
            flags[2] = HasBeatenInfernumProvRegularly;
            flags[3] = HasProvidenceDoorShattered;
            flags[4] = HasSepulcherAnimationBeenPlayed;
            flags[5] = InPostAEWUpdateWorld;
            flags[6] = HasDefeatedEidolists;
            flags[7] = DownedBereftVassal;

            flags2[0] = HasGeneratedProfanedShrine;
            flags2[1] = HasGeneratedColosseumEntrance;
            flags2[2] = PerformedLacewingAnimation;
            flags2[3] = MetSignusAtProfanedGarden;

            writer.Write(flags);
            writer.Write(flags2);

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
            writer.Write(WayfinderGateLocation.X);
            writer.Write(WayfinderGateLocation.Y);
            writer.Write(LostColosseumPortalAnimationTimer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            BitsByte flags2 = reader.ReadByte();
            InfernumModeEnabled = flags[0];
            HasBeatenInfernumNightProvBeforeDay = flags[1];
            HasBeatenInfernumProvRegularly = flags[2];
            HasProvidenceDoorShattered = flags[3];
            HasSepulcherAnimationBeenPlayed = flags[4];
            InPostAEWUpdateWorld = flags[5];
            HasDefeatedEidolists = flags[6];
            DownedBereftVassal = flags[7];

            HasGeneratedProfanedShrine = flags2[0];
            HasGeneratedColosseumEntrance = flags2[1];
            PerformedLacewingAnimation = flags2[2];
            MetSignusAtProfanedGarden = flags2[3];

            AbyssLayer1ForestSeed = reader.ReadInt32();
            AbyssLayer3CavernSeed = reader.ReadInt32();
            SquidDenCenter = new(reader.ReadInt32(), reader.ReadInt32());
            EidolistWorshipPedestalCenter = new(reader.ReadInt32(), reader.ReadInt32());

            ProvidenceArena = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

            WayfinderGateLocation = new(reader.ReadSingle(), reader.ReadSingle());
            LostColosseumPortalAnimationTimer = reader.ReadInt32();
        }
    }
}
