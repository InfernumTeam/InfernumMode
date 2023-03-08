using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class EggPlayer : ModPlayer
    {
        public float EggShieldOpacity;

        public int EggShieldCooldown;

        public const int EggShieldMaxCooldown = 1200;

        public bool EggShieldActive;

        public int CurrentEggShieldHits;

        public const int MaxEggShieldHits = 3;

        public void ToggleEggShield(bool status)
        {
            EggShieldActive = status;
            CurrentEggShieldHits = 0;

            if (status)
                EggShieldCooldown = EggShieldMaxCooldown;
        }

        public override void PreUpdate()
        {
            // Reduce the cooldown.
            if (EggShieldCooldown > 0)
                EggShieldCooldown--;

            // Deactivate the shield if half the cooldown has been reached.
            if (EggShieldActive && EggShieldCooldown == EggShieldMaxCooldown / 2)
                ToggleEggShield(false);

            // If the max hits have been taken, disable the shield and reset the current hits taken.
            if (CurrentEggShieldHits >= MaxEggShieldHits)
                ToggleEggShield(false);

            // Sort out the opacity.
            if (EggShieldActive)
                EggShieldOpacity = MathHelper.Clamp(EggShieldOpacity + 0.1f, 0f, 1f);
            else
                EggShieldOpacity = MathHelper.Clamp(EggShieldOpacity - 0.1f, 0f, 1f);
        }

        public override void PostUpdateEquips()
        {
            if (EggShieldActive)
            {
                Player.statDefense += 100;
                Player.GetDamage<GenericDamageClass>() *= 0.5f;
            }
        }

        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource, ref int cooldownCounter)
        {
            if (EggShieldActive)
                CurrentEggShieldHits++;
            return true;
        }
    }
}
