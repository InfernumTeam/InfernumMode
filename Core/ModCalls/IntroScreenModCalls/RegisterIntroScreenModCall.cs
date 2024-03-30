using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Luminance.Core.ModCalls;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class RegisterIntroScreenModCall : ModCall
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

        protected override object SafeProcess(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            screen.RegisterIntroScreen();
            return ModCallManager.DefaultObject;
        }
    }
}
