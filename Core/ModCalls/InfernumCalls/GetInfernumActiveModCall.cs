using System;
using System.Collections.Generic;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    internal class GetInfernumActiveModCall : ReturnValueModCall<bool>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "GetInfernumActive";
            }
        }

        public override IEnumerable<Type> InputTypes => null;

        protected override bool ProcessGeneric(params object[] argsWithoutCommand) => InfernumMode.CanUseCustomAIs;
    }
}
