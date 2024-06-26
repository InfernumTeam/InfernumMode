﻿using System;
using System.Collections.Generic;
using CalamityMod;
using InfernumMode.Core.GlobalInstances.Systems;
using Luminance.Core.ModCalls;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    public class CanPlaySoulHeadphonesMusicModCall : ModCall
    {
        public override IEnumerable<string> GetCallCommands()
		{
			yield return "CanPlaySoulHeadphonesMusic";
		}

        public override IEnumerable<Type> GetInputTypes()
		{
			yield return typeof(string);
		}

        protected override object SafeProcess(params object[] argsWithoutCommand)
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
