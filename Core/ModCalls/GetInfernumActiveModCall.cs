using System;
using System.Collections.Generic;
using Luminance.Core.ModCalls;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    internal class GetInfernumActiveModCall : ModCall
    {
        public override IEnumerable<string> GetCallCommands()
		{
			yield return "GetInfernumActive";
		}

        public override IEnumerable<Type> GetInputTypes()
        {
            return null;
        }

        protected override object SafeProcess(params object[] argsWithoutCommand) => InfernumMode.CanUseCustomAIs;
    }
}
