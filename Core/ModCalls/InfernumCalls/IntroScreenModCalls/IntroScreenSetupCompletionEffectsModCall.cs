using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupCompletionEffectsModCall : ReturnValueModCall<ModCallIntroScreen>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "IntroScreenSetupCompletionEffects";
                yield return "SetupCompletionEffects";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
                yield return typeof(Action);
            }
        }

        protected override ModCallIntroScreen ProcessGeneric(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupCompletionEffects((Action)argsWithoutCommand[1]);
        }
    }
}
