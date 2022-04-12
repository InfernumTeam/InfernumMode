using System;

namespace InfernumMode
{
    public class InfernumModCalls
    {
        public static object Call(params object[] args)
        {
            if (args is null || args.Length <= 0)
                return new ArgumentNullException("ERROR: No function name specified. First argument must be a function name.");
            if (args[0] is not string)
                return new ArgumentException("ERROR: First argument must be a string function name.");

            string methodName = args[0].ToString();
            switch (methodName)
            {
                case "GetInfernumActive":
                    return PoDWorld.InfernumMode;
                case "SetInfernumActive":
                    PoDWorld.InfernumMode = (bool)args[1];
                    break;
            }
            return null;
        }
    }
}
