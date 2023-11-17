using System;
using System.Linq;
using System.Collections.Generic;

namespace InfernumMode.Core.ModCalls
{
    // Check the wiki for documentation on these. https://infernummod.wiki.gg
    public class InfernumModCalls
    {
        internal static List<ModCall> ModCalls = new();

        public static object Call(params object[] args)
        {
            if (args is null || args.Length <= 0)
                return new ArgumentException("ERROR: No function name specified. First argument must be a function name.");
            if (args[0] is not string command)
                return new ArgumentException("ERROR: First argument must be a string function name.");

            foreach (var modCall in ModCalls)
            {
                if (modCall.CallCommands.Any(callCommand => callCommand.Equals(command, StringComparison.OrdinalIgnoreCase)))
                    return modCall.ProcessInternal(args.Skip(1).ToArray());
            }
            return null;
        }
    }
}
