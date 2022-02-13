using System;
using System.Collections.Generic;
using System.Reflection;

namespace InfernumMode
{
    public static partial class Utilities
    {
        /// <summary>
        /// Binding flags that account for all access/local membership status.
        /// </summary>
        public static readonly BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// Retrieves all types which derive from a specific type in a given assembly.
        /// </summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="assemblyToSearch">The assembly to search.</param>
        public static IEnumerable<Type> GetEveryMethodDerivedFrom(Type baseType, Assembly assemblyToSearch)
        {
            foreach (Type type in assemblyToSearch.GetTypes())
            {
                if (!type.IsSubclassOf(baseType) || type.IsAbstract)
                    continue;

                yield return type;
            }
        }
    }
}
