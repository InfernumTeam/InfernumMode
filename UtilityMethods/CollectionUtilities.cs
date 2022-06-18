namespace InfernumMode
{
    public static partial class Utilities
    {
        public static T[] Fuse<T>(this T[] c1, params T[] c2)
        {
            T[] result = new T[c1.Length + c2.Length];
            for (int i = 0; i < c1.Length; i++)
                result[i] = c1[i];
            for (int i = 0; i < c2.Length; i++)
                result[i + c1.Length] = c2[i];
            return result;
        }
    }
}
