namespace InfernumMode
{
    public static partial class Utilities
    {
        public static string AddOrdinalSuffix(int num)
        {
            if (num <= 0)
                return num.ToString();

            return (num % 100) switch
            {
                11 or 12 or 13 => num + "th",
                _ => (num % 10) switch
                {
                    1 => num + "st",
                    2 => num + "nd",
                    3 => num + "rd",
                    _ => num + "th",
                },
            };
        }

        public static string GetPath(this object obj) => obj.GetType().Namespace.Replace('.', '/') + '/' + obj.GetType().Name;
    }
}
