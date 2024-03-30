using System;
using System.Collections.Generic;
using Luminance.Core.ModCalls;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    internal class GetInfernumActiveModCall : ModCall
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "GetInfernumActive";
            }
        }

        public override IEnumerable<Type> InputTypes => null;

        protected override object SafeProcess(params object[] argsWithoutCommand) => InfernumMode.CanUseCustomAIs;
    }
}
