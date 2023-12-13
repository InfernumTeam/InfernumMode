using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using CalamityMod.Particles;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
    // Dedicated to: Pengolin, Fire Devourer
    public class Purity : ModItem
    {
        public const string FieldName = "Purity";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) => player.SetValue<bool>(FieldName, false);

            InfernumPlayer.AccessoryUpdateEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>(FieldName))
                {
                    // Everything but summoner.
                    player.Player.GetDamage<MeleeDamageClass>() *= 1.4f;
                    player.Player.GetDamage<MeleeNoSpeedDamageClass>() *= 1.4f;
                    player.Player.GetDamage<RangedDamageClass>() *= 1.4f;
                    player.Player.GetDamage<MagicDamageClass>() *= 1.4f;
                    player.Player.GetDamage<MagicSummonHybridDamageClass>() *= 1.4f;
                    player.Player.GetDamage<SummonMeleeSpeedDamageClass>() *= 1.4f;

                    player.Player.GetAttackSpeed<GenericDamageClass>() += 0.4f;
                    player.Player.buffImmune[ModContent.BuffType<Nightwither>()] = true;
                }
            };

            InfernumPlayer.ModifyHitNPCWithItemEvent += (InfernumPlayer player, Item item, NPC target, ref NPC.HitModifiers modifiers) =>
            {
                if (player.GetValue<bool>(FieldName))
                {
                    // Lie and check if it was a crit seperately because its not possible else.
                    float crit = player.Player.GetWeaponCrit(item);
                    if (Main.rand.Next(0, 101) < crit)
                        OnHitParticles(target);

                    modifiers.DisableCrit();
                }
            };

            InfernumPlayer.ModifyHitNPCWithProjEvent += (InfernumPlayer player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers) =>
            {
                if (player.GetValue<bool>(FieldName))
                {
                    // Lie and check if it was a crit seperately because its not possible else.
                    float crit = player.Player.GetWeaponCrit(player.Player.HeldItem);
                    if (Main.rand.Next(0, 101) < crit)
                        OnHitParticles(target);

                    modifiers.DisableCrit();
                }
            };
        }

        private static void OnHitParticles(NPC npc)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Main.rand.NextVector2FromRectangle(npc.Hitbox);
                Vector2 velocity = npc.SafeDirectionTo(position) * Main.rand.NextFloat(1f, 3f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(position, velocity, Color.Lerp(Color.LightBlue, Color.LightCyan, Main.rand.NextFloat(1f)),
                    Color.Lerp(Color.Cyan, Color.LightBlue, Main.rand.NextFloat()), Main.rand.NextFloat(0.3f, 0.5f), 40, Main.rand.NextFloat(-0.05f, 0.05f), 5f));
            }
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 60;
            Item.height = 56;
            Item.rare = ModContent.RarityType<InfernumPurityRarity>();
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => player.Infernum().SetValue<bool>(FieldName, true);

        public override void AddRecipes()
        {
            CreateRecipe().
                AddTile(TileID.LunarCraftingStation).
                AddIngredient(ModContent.ItemType<LunarCoin>()).
                AddIngredient(ModContent.ItemType<ExodiumCluster>(), 25).
                AddIngredient(ModContent.ItemType<CoreofEleum>(), 10).
                Register();
        }
    }
}
