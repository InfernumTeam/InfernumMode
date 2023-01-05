using CalamityMod.CalPlayer;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.Players
{
    public class NurseAnticheesePlayer : ModPlayer
    {
        public override bool ModifyNurseHeal(NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText)
        {
            if (InfernumMode.CanUseCustomAIs && CalamityPlayer.areThereAnyDamnBosses)
            {
                chatText = "I cannot help you. Good luck.";
                return false;
            }
            return true;
        }
    }
}