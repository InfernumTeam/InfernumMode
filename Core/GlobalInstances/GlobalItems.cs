using CalamityMod.CalPlayer;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.Items;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalItems : GlobalItem
    {
        public static Dictionary<int, string> EnrageTooltipReplacements => new()
        {
            [ModContent.ItemType<DecapoditaSprout>()] = "Enrages outside of the Mushroom biome",
            [ItemID.WormFood] = "Enrages outside of the Corruption",
            [ItemID.BloodySpine] = "Enrages outside of the Crimson",
            [ModContent.ItemType<Teratoma>()] = "Enrages outside of the Corruption",
            [ModContent.ItemType<BloodyWormFood>()] = "Enrages outside of the Crimson",
            [ItemID.MechanicalEye] = null,
            [ItemID.MechanicalSkull] = null,
            [ItemID.MechanicalWorm] = null,
            [ItemID.ClothierVoodooDoll] = null,
            [ModContent.ItemType<ExoticPheromones>()] = null,
            [ModContent.ItemType<NecroplasmicBeacon>()] = "Enrages outside of the Underground",
        };

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.CelestialSigil)
            {
                item.consumable = false;
                item.maxStack = 1;
            }

            bool isGSSItem = item.type == ModContent.ItemType<GrandScale>() || item.type == ModContent.ItemType<DuststormInABottle>() || item.type == ModContent.ItemType<SandSharknadoStaff>() ||
                item.type == ModContent.ItemType<Sandslasher>() || item.type == ModContent.ItemType<SandstormGun>() || item.type == ModContent.ItemType<ShiftingSands>() || item.type == ModContent.ItemType<Tumbleweed>() ||
                item.type == ModContent.ItemType<SandSharkToothNecklace>() || item.type == ModContent.ItemType<SandstormsCore>();

            if (isGSSItem)
                item.rare = ItemRarityID.Cyan;

            if (ItemDamageValues.DamageValues.TryGetValue(item.type, out int newDamage))
                item.damage = newDamage;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.CelestialSigil)
            {
                var tooltip0 = tooltips.FirstOrDefault(x => x.Name == "Tooltip0" && x.Mod == "Terraria");
                if (tooltip0 != null)
                {
                    tooltip0.Text = 
                        "Summons the Moon Lord immediately\n" +
                        "Creates an arena at the player's position\n" +
                        "Not consumable.";
                }
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedShard>())
            {
                var tooltip1 = tooltips.FirstOrDefault(x => x.Name == "Tooltip1" && x.Mod == "Terraria");
                if (tooltip1 != null)
                {
                    tooltip1.Text = "Summons the Profaned Guardians when used in the profaned garden at the far right of the underworld";

                    tooltips.RemoveAt(tooltips.IndexOf(tooltip1) + 1);

                    // Warn the player about not having a profaned garden if it hasn't been generated yet.
                    if (!WorldSaveSystem.HasGeneratedProfanedShrine)
                    {
                        TooltipLine warningTooltip = new(Mod, "Warning",
                            "Your world does not currently have a Profaned Garden. Kill the Moon Lord again to generate it\n" +
                            "Be sure to grab the Hell schematic first if you do this, as the garden might destroy the lab");
                        warningTooltip.OverrideColor = Color.Orange;
                        tooltips.Insert(tooltips.IndexOf(tooltip1) + 1, warningTooltip);
                    }
                }
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedCore>())
            {
                var tooltip1 = tooltips.FirstOrDefault(x => x.Name == "Tooltip1" && x.Mod == "Terraria");
                if (tooltip1 != null)
                    tooltip1.Text = "Summons Providence when used at the alter in the profaned temple at the far right of the underworld";
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.LihzahrdPowerCell)
            {
                var tooltip0 = tooltips.FirstOrDefault(x => x.Name == "Tooltip0" && x.Mod == "Terraria");
                if (tooltip0 != null)
                    tooltip0.Text += "\nCreates a rectangular arena around the altar. If the altar is inside of the temple solid tiles within the arena are broken";
            }

            if (item.type == ModContent.ItemType<SandstormsCore>())
            {
                var tooltip0 = tooltips.FirstOrDefault(x => x.Name == "Tooltip0" && x.Mod == "Terraria");
                if (tooltip0 != null)
                {
                    if (WorldSaveSystem.HasGeneratedColosseumEntrance || SubworldSystem.IsActive<LostColosseum>())
                    {
                        tooltip0.Text = "Opens a portal to the Lost Colosseum";
                        tooltip0.OverrideColor = Color.Lerp(Color.Orange, Color.Yellow, 0.55f);
                    }

                    // Warn the player about not having a colosseum entrance if it hasn't been generated yet.
                    else
                    {
                        tooltip0.Text = "Your world does not currently have a Lost Gateway. Kill the Lunatic Cultist again to generate it.";
                        tooltip0.OverrideColor = Color.Orange;
                    }
                }
                tooltips.RemoveAll(x => x.Name == "Tooltip1" && x.Mod == "Terraria");
            }

            // Remove a bunch of "Enrages in XYZ" tooltips from base Calamity because people keep getting confused by it.
            if (InfernumMode.CanUseCustomAIs && EnrageTooltipReplacements.TryGetValue(item.type, out string tooltipReplacement))
            {
                var enrageTooltip = tooltips.FirstOrDefault(x => x.Text.Contains("enrage", StringComparison.OrdinalIgnoreCase));
                if (enrageTooltip != null)
                {
                    int enrageTextStart = enrageTooltip.Text.IndexOf("enrage", StringComparison.OrdinalIgnoreCase);
                    int enrageTextEnd = enrageTextStart;

                    // Find where the current line terminates following the instance of the word 'enrage'.
                    while (enrageTextEnd < enrageTooltip.Text.Length && enrageTooltip.Text[enrageTextEnd] != '\n')
                        enrageTextEnd++;

                    enrageTooltip.Text = enrageTooltip.Text.Remove(enrageTextStart, Math.Min(enrageTextEnd - enrageTextStart + 1, enrageTooltip.Text.Length));

                    // If a replacement exists, insert it into the enrage text instead.
                    if (tooltipReplacement is not null)
                        enrageTooltip.Text = enrageTooltip.Text.Insert(enrageTextStart, tooltipReplacement);
                }
            }
        }

        internal static void DoGTeleportDenialText(Player player, Item item)
        {
            if (!player.chaosState)
            {
                player.AddBuff(BuffID.ChaosState, CalamityPlayer.chaosStateDuration, true);
                Projectile.NewProjectile(player.GetSource_ItemUse(item), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<RoDFailPulse>(), 0, 0f, player.whoAmI);

                string[] possibleEdgyShitToSay = new string[]
                {
                    "YOU CANNOT EVADE ME SO EASILY!",
                    "YOU CANNOT HOPE TO OUTSMART A MASTER OF DIMENSIONS!",
                    "NOT SO FAST!"
                };
                Utilities.DisplayText(Main.rand.Next(possibleEdgyShitToSay), Color.Cyan);
                HatGirl.SayThingWhileOwnerIsAlive(player, "It seems as if it is manipulating telelocational magic, your Rod of Discord is of no use here!");
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.RodofDiscord && NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
            {
                DoGTeleportDenialText(player, item);
                return false;
            }

            if (InfernumMode.CanUseCustomAIs && (item.type == ModContent.ItemType<ProfanedShard>() || item.type == ModContent.ItemType<ProfanedCore>() || item.type == ModContent.ItemType<SandstormsCore>()))
                return false;

            bool inArena = player.Infernum_Biome().InProfanedArenaAntiCheeseZone || SubworldSystem.IsActive<LostColosseum>();
            bool illegalItemForArena = item.type is ItemID.Sandgun or ItemID.DirtBomb or ItemID.DirtStickyBomb or ItemID.DryBomb;
            if (illegalItemForArena && inArena)
                return false;

            return base.CanUseItem(item, player);
        }
        public override bool? UseItem(Item item, Player player)
        {
            if (item.type == ItemID.CelestialSigil && !NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                NPC.NewNPC(player.GetSource_ItemUse(item), (int)player.Center.X, (int)player.Center.Y, NPCID.MoonLordCore);
            }

            return base.UseItem(item, player);
        }

        public override bool OnPickup(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<DemonicChaliceOfInfernum>())
                AchievementPlayer.ExtraUpdateAchievements(player, new UpdateContext(-1, item.type));
            return true;
        }
    }
}
