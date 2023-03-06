using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class UIPlayer : ModPlayer
    {
        public bool DrawPlaqueUI = false;

        public override void ResetEffects() { }

        public override void UpdateDead() { }
    }
}
