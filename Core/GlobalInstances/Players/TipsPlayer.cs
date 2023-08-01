using InfernumMode.Core.GlobalInstances.Systems;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class TipsPlayer : ModPlayer
    {
        public bool HatGirl
        {
            get;
            set;
        }

        public bool ShouldDisplayTips
        {
            get;
            set;
        }

        public override void ResetEffects() => HatGirl = false;

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            TipsManager.PotentialTipToUse = TipsManager.SelectTip();
        }
    }
}
