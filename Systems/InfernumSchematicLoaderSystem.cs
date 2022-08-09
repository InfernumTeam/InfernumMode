using CalamityMod.Schematics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class InfernumSchematicLoaderSystem : ModSystem
    {
        internal static Dictionary<string, SchematicMetaTile[,]> TileMaps =>
            typeof(SchematicManager).GetField("TileMaps", Utilities.UniversalBindingFlags).GetValue(null) as Dictionary<string, SchematicMetaTile[,]>;

        internal static readonly MethodInfo ImportSchematicMethod = typeof(CalamitySchematicIO).GetMethod("ImportSchematic", Utilities.UniversalBindingFlags);

        public override void OnModLoad()
        {
            TileMaps["Profaned Arena"] = LoadInfernumSchematic("Schematics/ProfanedArena.csch");
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