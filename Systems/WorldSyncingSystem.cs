using CalamityMod.Waters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class WorldSyncingSystem : ModSystem
    {
        internal static List<CustomLavaStyle> CustomLavaStyles
        {
            get => (List<CustomLavaStyle>)typeof(CustomLavaManagement).GetField("CustomLavaStyles", Utilities.UniversalBindingFlags).GetValue(null);
            set => typeof(CustomLavaManagement).GetField("CustomLavaStyles", Utilities.UniversalBindingFlags).SetValue(null, value);
        }

        internal static MethodInfo LoadMethod = typeof(CustomLavaStyle).GetMethod("Load", Utilities.UniversalBindingFlags);

        public override void Load()
        {
            CustomLavaStyles ??= new();
            foreach (Type type in typeof(InfernumMode).Assembly.GetTypes())
            {
                // Ignore abstract types; they cannot have instances.
                // Also ignore types which do not derive from CustomLavaStyle.
                if (!type.IsSubclassOf(typeof(CustomLavaStyle)) || type.IsAbstract)
                    continue;

                CustomLavaStyles.Add(Activator.CreateInstance(type) as CustomLavaStyle);
                LoadMethod.Invoke(CustomLavaStyles.Last(), Array.Empty<object>());
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            BitsByte flags = new();
            flags[0] = WorldSaveSystem.InfernumMode;
            flags[1] = WorldSaveSystem.HasBeatedInfernumNightProvBeforeDay;
            flags[2] = WorldSaveSystem.HasBeatedInfernumProvRegularly;
            flags[3] = WorldSaveSystem.HasProvidenceDoorShattered;
            writer.Write(flags);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            WorldSaveSystem.InfernumMode = flags[0];
            WorldSaveSystem.HasBeatedInfernumNightProvBeforeDay = flags[1];
            WorldSaveSystem.HasBeatedInfernumProvRegularly = flags[2];
            WorldSaveSystem.HasProvidenceDoorShattered = flags[3];
        }
    }
}