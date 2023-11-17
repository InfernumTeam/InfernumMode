using System;
using System.Collections.Generic;
using InfernumMode.Core.GlobalInstances.Systems;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    public class SetInfernumActiveModCall : GenericModCall
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "SetInfernumActive";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(bool);
            }
        }

        protected override void ProcessGeneric(params object[] argsWithoutCommand) => WorldSaveSystem.InfernumModeEnabled = (bool)argsWithoutCommand[0];
    }
}
