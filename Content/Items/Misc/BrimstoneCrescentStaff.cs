using CalamityMod;
using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    // Dedicated to: Myra
    public class BrimstoneCrescentStaff : ModItem
    {
        public static int SpinTime => 54;

        public static int RaiseUpwardsTime => 27;

        public static int DebuffTime => 15;

        public static int ExplosionBaseDamage => 600;

        public static int MaxForcefieldHits => 3;

        public static int ForcefieldCreationDelayAfterBreak => 45;

        public static float ForcefieldDRMultiplier => 0.6f;

        public static float DamageMultiplier => 0.4f;

        public override void SetStaticDefaults()
        {
            InfernumPlayer.UpdateDeadEvent += (InfernumPlayer player) => player.SetValue<bool>("BrimstoneCrescentForcefieldIsActive", false);

            InfernumPlayer.AccessoryUpdateEvent += (InfernumPlayer player) =>
            {
                player.SetValue<float>("BrimstoneCrescentForcefieldStrengthInterpolant", Clamp(player.GetValue<float>("BrimstoneCrescentForcefieldStrengthInterpolant") + player.GetValue<bool>("BrimstoneCrescentForcefieldIsActive").ToDirectionInt() * 0.02f, 0f, 1f));
                if (player.GetValue<bool>("BrimstoneCrescentForcefieldIsActive"))
                    player.Player.AddBuff(ModContent.BuffType<BrimstoneBarrier>(), CalamityUtils.SecondsToFrames(DebuffTime));
            };

            InfernumPlayer.ModifyHurtEvent += (InfernumPlayer player, ref Player.HurtModifiers modifiers) =>
            {
                var hits = player.GetRefValue<int>("BrimstoneCrescentForcefieldHits");
                if (player.GetValue<bool>("BrimstoneCrescentForcefieldIsActive"))
                {
                    // Apply DR and disable typical hit graphical/sound effects.
                    modifiers.FinalDamage *= (1f - ForcefieldDRMultiplier);

                    // Play a custom fire hit effect.
                    SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath, player.Player.Center);

                    int explosionDamage = (int)player.Player.GetBestClassDamage().ApplyTo(ExplosionBaseDamage);
                    if (Main.myPlayer == player.Player.whoAmI)
                        Projectile.NewProjectile(player.Player.GetSource_OnHurt(player.Player), player.Player.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneForcefieldExplosion>(), explosionDamage, 0f, player.Player.whoAmI, 0f, 100f);

                    // Break the forcefield once it incurs enough hits.
                    hits.Value++;
                    if (hits.Value >= MaxForcefieldHits)
                    {
                        player.Player.AddBuff(ModContent.BuffType<BrimstoneExhaustion>(), CalamityUtils.SecondsToFrames(ForcefieldCreationDelayAfterBreak));
                        hits.Value = 0;
                        player.SetValue<bool>("BrimstoneCrescentForcefieldIsActive", false);
                    }
                }
                else
                    hits.Value = 0;
            };
        }

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 114;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<BrimstoneCrescentStaffProj>();

            Item.value = ItemRarityID.LightPurple;
            Item.rare = ModContent.RarityType<InfernumScarletSparkleRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
