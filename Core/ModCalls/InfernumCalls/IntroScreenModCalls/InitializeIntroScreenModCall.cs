using System;
using System.Collections.Generic;
using InfernumMode.Content.BossIntroScreens;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace InfernumMode.Core.ModCalls.InfernumCalls.IntroScreenModCalls
{
    /// <summary>
    /// Initializes up a <see cref="ModCallIntroScreen"/> for setting up.
    /// </summary>
    public class InitializeIntroScreenModCall : ReturnValueModCall<ModCallIntroScreen>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "InitializeIntroScreen";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(LocalizedText);
                yield return typeof(int);
                yield return typeof(bool);
                yield return typeof(Func<bool>);
                yield return typeof(Func<float, float, Color>);
            }
        }

        protected override ModCallIntroScreen ProcessGeneric(params object[] argsWithoutCommand)
        {
            return ModCallIntroScreen.InitializeNewModCallIntroScreen(
                textToDisplay: (LocalizedText)argsWithoutCommand[0],
                animationTime: (int)argsWithoutCommand[1],
                textShouldBeCentered: (bool)argsWithoutCommand[2],
                shouldBeActive: (Func<bool>)argsWithoutCommand[3],
                textColor: (Func<float, float, Color>)argsWithoutCommand[4]);
        }
    }
}
