using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using CalamityMod.Particles;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
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
                    player.Player.GetDamage<GenericDamageClass>() += 0.3f;
                    player.Player.GetAttackSpeed<GenericDamageClass>() += 0.3f;
                    player.Player.buffImmune[ModContent.BuffType<Nightwither>()] = true;
                }
            };

            InfernumPlayer.ModifyHitNPCWithItemEvent += (InfernumPlayer player, Item item, NPC target, ref NPC.HitModifiers modifiers) =>
            {
                if (player.GetValue<bool>(FieldName))
                {
                    modifiers.ModifyHitInfo += (ref NPC.HitInfo info) =>
                    {
                        if (info.Crit)
                            OnHitParticles(target);
                    };
                    modifiers.DisableCrit();
                }
            };

            InfernumPlayer.ModifyHitNPCWithProjEvent += (InfernumPlayer player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers) =>
            {
                if (player.GetValue<bool>(FieldName))
                {
                    modifiers.ModifyHitInfo += (ref NPC.HitInfo info) =>
                    {
                        if (info.Crit)
                            OnHitParticles(target);
                    };

                    // Lie and check if it was a crit seperately because its not possible else.
                    float crit = player.Player.GetWeaponCrit(player.Player.HeldItem);
                    if (Main.rand.Next(0, 101) < crit)
                        OnHitParticles(target);

                    modifiers.DisableCrit();
                }
            };
        }

        private void OnHitParticles(NPC npc)
        {
            //var bloom = new GenericBloom(Main.rand.NextVector2FromRectangle(npc.Hitbox), Vector2.Zero, Color.Lerp(Color.Cyan, Color.LightBlue, Main.rand.NextFloat()), Main.rand.NextFloat(0.5f, 0.7f), 15, false);
            //GeneralParticleHandler.SpawnParticle(bloom);

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

        public override void UpdateAccessory(Player player, bool hideVisual) => player.Infernum().SetValue<bool>("Purity", true);

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
