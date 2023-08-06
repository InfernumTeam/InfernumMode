using CalamityMod;
using InfernumMode.Content.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.Items.Accessories.CherishedSealocket;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class SealocketPlayer : ModPlayer
    {
        public bool MechanicalEffectsApply
        {
            get;
            set;
        }

        public bool ForcefieldCanDraw
        {
            get;
            set;
        }

        public int RemainingHits
        {
            get;
            set;
        } = MaxHighDRHits;

        public float ForcefieldDissipationInterpolant
        {
            get;
            set;
        } = 1f;

        public float ForcefieldOpacity
        {
            get;
            set;
        }

        public bool ForcefieldShouldDraw => (ForcefieldCanDraw || ForcefieldOpacity > 0f) && RemainingHits >= 1 && !Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _);

        public override void ResetEffects()
        {
            if (!MechanicalEffectsApply)
                RemainingHits = MaxHighDRHits;

            MechanicalEffectsApply = false;
            ForcefieldCanDraw = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // Make the forcefield dissipate.
            bool hasCooldown = Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _);
            bool dissipate = hasCooldown || !ForcefieldCanDraw;
            if (!ForcefieldCanDraw)
                ForcefieldOpacity *= 0.9f;
            ForcefieldOpacity = Clamp(ForcefieldOpacity - hasCooldown.ToDirectionInt() * 0.025f, 0f, 1f);
            ForcefieldDissipationInterpolant = Clamp(ForcefieldDissipationInterpolant + dissipate.ToDirectionInt() * 0.023f, 0f, 1f);

            if (!MechanicalEffectsApply)
                return;

            // Apply the recharge cooldown if all hits have been exhausted.
            if (RemainingHits <= 0 && !hasCooldown)
            {
                Player.AddCooldown(SealocketForcefieldRecharge.ID, CalamityUtils.SecondsToFrames(ForcefieldRechargeSeconds));
                RemainingHits = MaxHighDRHits;
            }

            // Apply the damage boost if the cooldown is not up.
            if (!hasCooldown)
                Player.GetDamage<GenericDamageClass>() += DamageBoostWhenForcefieldIsUp;

            if (ForcefieldShouldDraw)
                Lighting.AddLight(Player.Center, Color.Cyan.ToVector3() * 0.24f);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            modifiers.ModifyHurtInfo += Modifiers_ModifyHurtInfo;

            if (MechanicalEffectsApply && !Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _) && ShouldReduceDamage)
            {
                // Apply DR and disable typical hit sound effects.
                modifiers.FinalDamage *= (1f - ForcefieldDRMultiplier);

                RemainingHits--;
                // Play a custom water wobble effect.
                SoundEngine.PlaySound(SoundID.Item130, Player.Center);
            }
        }

        private bool ShouldReduceDamage;

        private void Modifiers_ModifyHurtInfo(ref Player.HurtInfo info)
        {
            if (info.Damage >= 120)
                ShouldReduceDamage = true;
            else
                ShouldReduceDamage = false;
        }

        // Reset the hit counter if the player died and is about to respawn.
        public override void UpdateDead() => RemainingHits = MaxHighDRHits;
    }
}
