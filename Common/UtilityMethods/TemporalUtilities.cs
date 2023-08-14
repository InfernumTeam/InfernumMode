using System;

namespace InfernumMode
{
    public static partial class Utilities
    {
        #pragma warning disable CA2211
        public static bool OverrideAprilFirst;
        #pragma warning restore CA2211

        public static bool IsAprilFirst() => (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) || OverrideAprilFirst;
    }
}
