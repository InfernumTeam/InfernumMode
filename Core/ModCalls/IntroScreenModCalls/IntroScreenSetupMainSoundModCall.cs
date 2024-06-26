﻿using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Luminance.Core.ModCalls;
using Terraria.Audio;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupMainSoundModCall : ModCall
    {
        public override IEnumerable<string> GetCallCommands()
		{
			yield return "IntroScreenSetupMainSound";
			yield return "SetupMainSound";
		}

        public override IEnumerable<Type> GetInputTypes()
        {
            yield return typeof(ModCallIntroScreen);
            yield return typeof(Func<int, int, float, float, bool>);
            yield return typeof(Func<SoundStyle>);
        }

        protected override object SafeProcess(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupMainSound((Func<int, int, float, float, bool>)argsWithoutCommand[1], (Func<SoundStyle>)argsWithoutCommand[2]);
        }
    }
}
