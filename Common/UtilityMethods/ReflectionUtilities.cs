using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Terraria.ModLoader.Core;

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
        public static IEnumerable<Type> GetEveryTypeDerivedFrom(Type baseType, Assembly assemblyToSearch)
        {
            foreach (Type type in AssemblyManager.GetLoadableTypes(assemblyToSearch))
            {
                if (!type.IsSubclassOf(baseType) || type.IsAbstract)
                    continue;

                yield return type;
            }
        }

        /// <summary>
        /// Converts a <see cref="MethodInfo"/> to a generic <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="instance">The instance to create the delegate around.</param>
        public static Delegate ConvertToDelegate(this MethodInfo method, object instance)
        {
            List<Type> paramTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToList();
            paramTypes.Add(method.ReturnType);

            Type delegateType = Expression.GetDelegateType(paramTypes.ToArray());
            return Delegate.CreateDelegate(delegateType, instance, method);
        }
    }
}
