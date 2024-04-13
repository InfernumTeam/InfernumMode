using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Luminance.Core.ModCalls;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupLetterDisplayCompletionRatioModCall : ModCall
    {
        public override IEnumerable<string> GetCallCommands()
		{
			yield return "IntroScreenSetupLetterDisplayCompletionRatio";
			yield return "SetupLetterDisplayCompletionRatio";
		}

        public override IEnumerable<Type> GetInputTypes()
        {
            yield return typeof(ModCallIntroScreen);
            yield return typeof(Func<int, float>);
        }

        protected override object SafeProcess(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupLetterDisplayCompletionRatio((Func<int, float>)argsWithoutCommand[1]);
        }
    }
}
