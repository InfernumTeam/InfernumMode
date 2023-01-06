using CalamityMod;
using InfernumMode.Content.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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
            ForcefieldOpacity = MathHelper.Clamp(ForcefieldOpacity - hasCooldown.ToDirectionInt() * 0.025f, 0f, 1f);
            ForcefieldDissipationInterpolant = MathHelper.Clamp(ForcefieldDissipationInterpolant + dissipate.ToDirectionInt() * 0.023f, 0f, 1f);

            // Reset cooldowns and hit counters if the accessory is no longer being used.
            if (!MechanicalEffectsApply)
            {
                if (Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out var cdDurability))
                    cdDurability.timeLeft = 0;
                RemainingHits = MaxHighDRHits;
                return;
            }

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

        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource, ref int cooldownCounter)
        {
            if (MechanicalEffectsApply && !Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _) && damage >= 120)
            {
                // Apply DR and disable typical hit graphical/sound effects.
                damage = (int)(damage * (1f - ForcefieldDRMultiplier));
                genGore = false;

                RemainingHits--;

                // Play a custom water wobble effect.
                SoundEngine.PlaySound(SoundID.Item130, Player.Center);
            }

            return true;
        }

        // Reset the hit counter if the player died and is about to respawn.
        public override void UpdateDead() => RemainingHits = MaxHighDRHits;
    }
}