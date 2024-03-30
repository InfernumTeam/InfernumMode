using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Luminance.Core.ModCalls;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupTextScaleModCall : ModCall
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "IntroScreenSetupTextScale";
                yield return "SetupTextScale";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
                yield return typeof(float);
            }
        }

        protected override object SafeProcess(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupTextScale((float)argsWithoutCommand[1]);
        }
    }
}
