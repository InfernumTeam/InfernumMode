using CalamityMod;
using CalamityMod.Items;
using InfernumMode.Common.DataStructures;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.Cooldowns;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
    public class CherishedSealocket : ModItem
    {
        public const int MaxHighDRHits = 2;

        public const int ForcefieldRechargeSeconds = 90;

        public const float ForcefieldDRMultiplier = 0.6f;

        public const float DamageBoostWhenForcefieldIsUp = 0.12f;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.OnEnterWorldEvent += (InfernumPlayer player) =>
            {
                player.SetValue<float>("SealocketForcefieldDissipationInterpolant", 1f);
                player.SetValue<int>("SealocketRemainingHits", 3);
            };

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) =>
            {
                player.SetValue<bool>("SealocketMechanicalEffectsApply", false);
                player.SetValue<bool>("SealocketForcefieldCanDraw", false);
            };

            InfernumPlayer.UpdateDeadEvent += (InfernumPlayer player) =>
            {
                player.SetValue<int>("SealocketRemainingHits", MaxHighDRHits);
            };

            InfernumPlayer.AccessoryUpdateEvent += (InfernumPlayer player) =>
            {
                // Make the forcefield dissipate.
                Referenced<float> forcefieldOpacity = player.GetRefValue<float>("SealocketForcefieldOpacity");
                Referenced<int> remainingHits = player.GetRefValue<int>("SealocketRemainingHits");
                Referenced<float> forcefieldDissipationInterpolant = player.GetRefValue<float>("SealocketForcefieldDissipationInterpolant");

                bool forcefieldCanDraw = player.GetValue<bool>("SealocketForcefieldCanDraw");

                bool hasCooldown = player.Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _);
                bool dissipate = hasCooldown || !forcefieldCanDraw;
                if (!forcefieldCanDraw)
                    forcefieldOpacity.Value *= 0.9f;
                forcefieldOpacity.Value = Clamp(forcefieldOpacity.Value - hasCooldown.ToDirectionInt() * 0.025f, 0f, 1f);
                forcefieldDissipationInterpolant.Value = Clamp(forcefieldDissipationInterpolant.Value + dissipate.ToDirectionInt() * 0.023f, 0f, 1f);

                if (!player.GetValue<bool>("SealocketMechanicalEffectsApply"))
                    return;

                // Apply the recharge cooldown if all hits have been exhausted.
                if (remainingHits.Value <= 0 && !hasCooldown)
                {
                    player.Player.AddCooldown(SealocketForcefieldRecharge.ID, CalamityUtils.SecondsToFrames(2));//ForcefieldRechargeSeconds));
                    remainingHits.Value = MaxHighDRHits;
                }

                // Apply the damage boost if the cooldown is not up.
                if (!hasCooldown)
                    player.Player.GetDamage<GenericDamageClass>() += DamageBoostWhenForcefieldIsUp;

                if (ForcefieldShouldDraw(player))
                    Lighting.AddLight(player.Player.Center, Color.Cyan.ToVector3() * 0.24f);
            };

            InfernumPlayer.ModifyHurtEvent += (InfernumPlayer player, ref Player.HurtModifiers modifiers) =>
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
                {
                    if (info.Damage >= 120)
                        player.SetValue<bool>("SealocketShouldReduceDamage", true);
                    else
                        player.SetValue<bool>("SealocketShouldReduceDamage", false);
                };

                Referenced<int> remainingHits = player.GetRefValue<int>("SealocketRemainingHits");

                if (player.GetValue<bool>("SealocketMechanicalEffectsApply") && !player.Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _) && player.GetValue<bool>("SealocketShouldReduceDamage"))
                {
                    // Apply DR and disable typical hit sound effects.
                    modifiers.FinalDamage *= (1f - ForcefieldDRMultiplier);

                    remainingHits.Value--;
                    // Play a custom water wobble effect.
                    SoundEngine.PlaySound(SoundID.Item130, player.Player.Center);
                    SoundEngine.PlaySound(SoundID.SplashWeak with { PitchVariance = 0.5f, Volume = 1.75f }, player.Player.Center);

                    bool finalHit = remainingHits.Value == 0;

                    int amount = 75;
                    if (InfernumConfig.Instance.ReducedGraphicsConfig)
                        amount /= 2;

                    float velocityCap = 3f;
                    float sizeModifier = 0.75f;

                    Vector2 position = player.Player.Center;
                    for (int i = 0; i < amount; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0f, velocityCap);
                        velocity.Y -= 3;
                        Vector2 size = new Vector2(Main.rand.NextFloat(0.9f, 1.1f), Main.rand.NextFloat(0.9f, 1.1f)) * Main.rand.NextFloat(36f, 45f) * sizeModifier;
                        ModContent.GetInstance<WaterMetaball>().SpawnParticle(position, velocity, size, Main.rand.NextFloat(0.93f, 0.94f));
                    }
                }
            };
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<InfernumVassalRarity>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Infernum().SetValue<bool>("SealocketMechanicalEffectsApply", true);
            player.Infernum().SetValue<bool>("SealocketForcefieldCanDraw", true);
        }

        public override void UpdateVanity(Player player) => player.Infernum().SetValue<bool>("SealocketForcefieldCanDraw", true);

        public static bool ForcefieldShouldDraw(InfernumPlayer player)
        {
            return (player.GetValue<bool>("SealocketForcefieldCanDraw") || player.GetValue<float>("SealocketForcefieldOpacity") > 0f) && player.GetValue<int>("SealocketRemainingHits") >= 1 && !player.Player.Calamity().cooldowns.TryGetValue(SealocketForcefieldRecharge.ID, out _);
        }
    }
}
