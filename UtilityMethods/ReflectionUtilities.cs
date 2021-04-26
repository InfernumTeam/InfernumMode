using System.Reflection;

namespace InfernumMode
{
    public static partial class Utilities
    {
        /// <summary>
        /// Binding flags that account for all access/local membership status.
        /// </summary>
        public static readonly BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
	}
}
