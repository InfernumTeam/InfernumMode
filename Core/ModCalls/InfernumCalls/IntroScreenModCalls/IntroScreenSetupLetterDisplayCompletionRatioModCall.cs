using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupLetterDisplayCompletionRatioModCall : ReturnValueModCall<ModCallIntroScreen>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "IntroScreenSetupLetterDisplayCompletionRatio";
                yield return "SetupLetterDisplayCompletionRatio";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
                yield return typeof(Func<int, float>);
            }
        }

        protected override ModCallIntroScreen ProcessGeneric(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupLetterDisplayCompletionRatio((Func<int, float>)argsWithoutCommand[1]);
        }
    }
}
