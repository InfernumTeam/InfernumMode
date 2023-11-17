using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    public class IntroScreenSetupLetterShaderModCall : ReturnValueModCall<ModCallIntroScreen>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "IntroScreenSetupLetterShader";
                yield return "SetupLetterShader";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(ModCallIntroScreen);
                yield return typeof(Effect);
                yield return typeof(Action<Effect>);
            }
        }

        protected override ModCallIntroScreen ProcessGeneric(params object[] argsWithoutCommand)
        {
            var screen = (ModCallIntroScreen)argsWithoutCommand[0];
            return screen.SetupLetterShader((Effect)argsWithoutCommand[1], (Action<Effect>)argsWithoutCommand[2]);
        }
    }
}
