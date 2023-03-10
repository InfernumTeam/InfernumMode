using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class CalCloneHexesPlayer : ModPlayer
    {
        public bool AcceleratingProjectilesHex
        {
            get;
            set;
        }

        public bool HomingProjectilesHex
        {
            get;
            set;
        }

        public bool LifeRegenDisablingHex
        {
            get;
            set;
        }

        public bool DefenseDRWeakeningHex
        {
            get;
            set;
        }
        
        public bool SeekerHarassmentHex
        {
            get;
            set;
        }

        #region Reset Effects
        public override void ResetEffects()
        {
            AcceleratingProjectilesHex = false;
            HomingProjectilesHex = false;
            LifeRegenDisablingHex = false;
            DefenseDRWeakeningHex = false;
            SeekerHarassmentHex = false;
        }
        #endregion Reset Effects

        #region Life Regen
        public override void UpdateBadLifeRegen()
        {
            if (LifeRegenDisablingHex && Player.lifeRegen >= 1)
            {
                Player.lifeRegen = 0;
                Player.lifeRegenTime = 0;
            }
        }
        #endregion Life Regen

        #region Updating
        public override void PostUpdate()
        {
            if (DefenseDRWeakeningHex)
            {
                Player.statDefense -= 35;
                Player.endurance = MathHelper.Clamp(Player.endurance * 0.5f, 0f, 0.25f);
            }
        }
        #endregion Updating
    }
}
