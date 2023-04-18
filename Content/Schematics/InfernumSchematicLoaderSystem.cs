using CalamityMod.Schematics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.ModLoader;

namespace InfernumMode.Content.Schematics
{
    public class InfernumSchematicLoaderSystem : ModSystem
    {
        internal static Dictionary<string, SchematicMetaTile[,]> TileMaps =>
            typeof(SchematicManager).GetField("TileMaps", Utilities.UniversalBindingFlags).GetValue(null) as Dictionary<string, SchematicMetaTile[,]>;

        internal static readonly MethodInfo ImportSchematicMethod = typeof(CalamitySchematicIO).GetMethod("ImportSchematic", Utilities.UniversalBindingFlags);

        public override void OnModLoad()
        {
            TileMaps["Profaned Arena"] = LoadInfernumSchematic("Content/Schematics/ProfanedArena.csch");
            TileMaps["LostColosseum"] = LoadInfernumSchematic("Content/Schematics/LostColosseum.csch");
            TileMaps["LostColosseumEntrance"] = LoadInfernumSchematic("Content/Schematics/LostColosseumEntrance.csch");
            TileMaps["LostColosseumExit"] = LoadInfernumSchematic("Content/Schematics/LostColosseumExit.csch");
            TileMaps["BlossomGarden"] = LoadInfernumSchematic("Content/Schematics/BlossomGarden.csch");
        }

        public static SchematicMetaTile[,] LoadInfernumSchematic(string filename)
        {
            SchematicMetaTile[,] ret = null;
            using (Stream st = InfernumMode.Instance.GetFileStream(filename, true))
                ret = (SchematicMetaTile[,])ImportSchematicMethod.Invoke(null, new object[] { st });

            return ret;
        }
    }
}