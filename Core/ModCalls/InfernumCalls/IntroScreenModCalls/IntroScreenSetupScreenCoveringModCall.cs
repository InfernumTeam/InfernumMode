using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Microsoft.Xna.Framework;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupScreenCoveringModCall : ReturnValueModCall<ModCallIntroScreen>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "IntroScreenSetupScreenCovering";
                yield return "SetupScreenCovering";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
                yield return typeof(Color);
            }
        }

        protected override ModCallIntroScreen ProcessGeneric(params object[] argsWithoutCommand)
        {
            ModCallIntroScreen screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupScreenCovering((Color)argsWithoutCommand[1]);
        }
    }
}
