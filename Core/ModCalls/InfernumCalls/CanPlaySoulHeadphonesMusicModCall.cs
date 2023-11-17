using System;
using System.Collections.Generic;
using CalamityMod;
using InfernumMode.Core.GlobalInstances.Systems;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    public class CanPlaySoulHeadphonesMusicModCall : ReturnValueModCall<bool>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "CanPlaySoulHeadphonesMusic";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(string);
            }
        }

        protected override bool ProcessGeneric(params object[] argsWithoutCommand)
        {
            return (string)argsWithoutCommand[0] switch
            {
                "BereftVassal" => WorldSaveSystem.DownedBereftVassal,
                "ExoMechs" => DownedBossSystem.downedExoMechs,
                _ => false,
            };
        }
    }
}
