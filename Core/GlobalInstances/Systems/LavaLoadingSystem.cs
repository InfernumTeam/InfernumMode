using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalamityMod.Waters;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class LavaLoadingSystem : ModSystem
    {
        internal static List<CustomLavaStyle> CustomLavaStyles
        {
            get => (List<CustomLavaStyle>)typeof(CustomLavaManagement).GetField("CustomLavaStyles", Utilities.UniversalBindingFlags).GetValue(null);
            set => typeof(CustomLavaManagement).GetField("CustomLavaStyles", Utilities.UniversalBindingFlags).SetValue(null, value);
        }

        internal static MethodInfo LoadMethod = typeof(CustomLavaStyle).GetMethod("Load", Utilities.UniversalBindingFlags);

        public override void Load()
        {
            CustomLavaStyles ??= [];
            foreach (Type type in typeof(InfernumMode).Assembly.GetTypes())
            {
                // Ignore abstract types; they cannot have instances.
                // Also ignore types which do not derive from CustomLavaStyle.
                if (!type.IsSubclassOf(typeof(CustomLavaStyle)) || type.IsAbstract)
                    continue;

                CustomLavaStyles.Add(Activator.CreateInstance(type) as CustomLavaStyle);
                LoadMethod.Invoke(CustomLavaStyles.Last(), []);
            }
        }
    }
}
