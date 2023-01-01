using InfernumMode.Systems;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.Players
{
    public class HatGirlTipsPlayer : ModPlayer
    {
        public bool HatGirl
        {
            get;
            set;
        }

        public bool HatGirlShouldGiveAdvice
        {
            get;
            set;
        }

        public override void ResetEffects() => HatGirl = false;

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            HatGirlTipsManager.PotentialTipToUse = HatGirlTipsManager.SelectTip();
        }
    }
}