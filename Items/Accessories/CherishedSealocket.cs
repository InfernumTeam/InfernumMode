using CalamityMod;
using CalamityMod.Items;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.Accessories
{
    public class CherishedSealocket : ModItem
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
            }
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
                if (MechanicalEffectsApply && !Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _) && damage >= 100)
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

        public const int MaxHighDRHits = 2;

        public const int ForcefieldRechargeSeconds = 90;

        public const float ForcefieldDRMultiplier = 0.8f;

        public const float DamageBoostWhenForcefieldIsUp = 0.12f;

        public override void Load() => On.Terraria.Main.DrawInfernoRings += DrawForcefields;

        public override void Unload() => On.Terraria.Main.DrawInfernoRings += DrawForcefields;

        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Cherished Sealocket");
            Tooltip.SetDefault($"Grants a water forcefield that dissipates after {MaxHighDRHits} hits\n" +
                $"When the forcefield is up, hard-hitting hits do {ForcefieldDRMultiplier * 100f}% less damage overall, and you recieve a {(int)(DamageBoostWhenForcefieldIsUp * 100f)}% damage boost\n" +
                $"The forcefield reappears after {ForcefieldRechargeSeconds} seconds");
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ItemRarityID.Cyan;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            SealocketPlayer modPlayer = player.GetModPlayer<SealocketPlayer>();
            modPlayer.MechanicalEffectsApply = true;
            modPlayer.ForcefieldCanDraw = !hideVisual;
        }

        public override void UpdateVanity(Player player) =>
            player.GetModPlayer<SealocketPlayer>().ForcefieldCanDraw = true;

        private void DrawForcefields(On.Terraria.Main.orig_DrawInfernoRings orig, Main self)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active || Main.player[i].outOfRange || Main.player[i].dead)
                    continue;

                SealocketPlayer modPlayer = Main.player[i].GetModPlayer<SealocketPlayer>();
                if (modPlayer.ForcefieldOpacity <= 0.01f || modPlayer.ForcefieldDissipationInterpolant >= 0.99f)
                    continue;

                float forcefieldOpacity = (1f - modPlayer.ForcefieldDissipationInterpolant) * modPlayer.ForcefieldOpacity;
                Vector2 forcefieldDrawPosition = Main.player[i].Center + Vector2.UnitY * Main.player[i].gfxOffY - Main.screenPosition;
                BereftVassal.DrawElectricShield(forcefieldOpacity, forcefieldDrawPosition, forcefieldOpacity, modPlayer.ForcefieldDissipationInterpolant * 1.5f + 1.3f);
            }
        }
    }
}
