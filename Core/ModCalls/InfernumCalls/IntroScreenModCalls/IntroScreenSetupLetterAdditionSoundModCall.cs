using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Terraria.Audio;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupLetterAdditionSoundModCall : ReturnValueModCall<ModCallIntroScreen>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "IntroScreenSetupLetterAdditionSound";
                yield return "SetupLetterAdditionSound";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
                yield return typeof(Func<SoundStyle>);
            }
        }

        protected override ModCallIntroScreen ProcessGeneric(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupLetterAdditionSound((Func<SoundStyle>)argsWithoutCommand[1]);
        }
    }
}
