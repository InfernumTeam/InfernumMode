using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class RegisterIntroScreenModCall : GenericModCall
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "RegisterIntroScreen";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
            }
        }

        protected override void ProcessGeneric(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            screen.RegisterIntroScreen();
        }
    }
}
