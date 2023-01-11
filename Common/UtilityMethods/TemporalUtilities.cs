using System;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static bool OverrideAprilFirst = false;

        public static bool IsAprilFirst() => (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) || OverrideAprilFirst;
    }
}
