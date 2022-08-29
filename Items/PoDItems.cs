using CalamityMod.CalPlayer;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class PoDItems : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.CelestialSigil)
            {
                item.consumable = false;
                item.maxStack = 1;
            }

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
                    if (!WorldSaveSystem.HasGeneratedProfanedShrine)
                    {
                        TooltipLine warningTooltip = new(Mod, "Warning",
                            "Your world does not currently have a garden. kill the Moon Lord again to generate it\n" +
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
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.RodofDiscord && NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
            {
                DoGTeleportDenialText(player, item);
                return false;
            }

            if (InfernumMode.CanUseCustomAIs && (item.type == ModContent.ItemType<ProfanedShard>() || item.type == ModContent.ItemType<ProfanedCore>()))
                return false;

            bool illegalItemForProvArena = item.type is ItemID.Sandgun or ItemID.DirtBomb or ItemID.DirtStickyBomb or ItemID.DryBomb;
            if (illegalItemForProvArena && player.Infernum().InProfanedArenaAntiCheeseZone)
                return false;

            return base.CanUseItem(item, player);
        }
        public override bool? UseItem(Item item, Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (item.type == ItemID.CelestialSigil && !NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                NPC.NewNPC(player.GetSource_ItemUse(item), (int)player.Center.X, (int)player.Center.Y, NPCID.MoonLordCore);
            }

            return base.UseItem(item, player);
        }

        /*
        public override void RightClick(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<StarterBag>())
            {
                DropHelper.DropItem(player.GetSource_OpenItem(item.type), player, ModContent.ItemType<BlastedTophat>());
                DropHelper.DropItemCondition(player.GetSource_OpenItem(item.type), player, ModContent.ItemType<DemonicChaliceOfInfernum>(), Main.expertMode);
            }
        }
        */

        public override void OpenVanillaBag(string context, Player player, int arg)
        {
            // Only apply bag drop contents in Infernum Mode and on boss bags.
            if (context != "bossBag" || !InfernumMode.CanUseCustomAIs)
                return;

            if (arg == ItemID.EaterOfWorldsBossBag)
            {
                int itemCount = Main.rand.Next(30, 60);
                player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.EaterOfWorldsBossBag), ItemID.DemoniteOre, itemCount);
                itemCount = Main.rand.Next(10, 20);
                player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.EaterOfWorldsBossBag), ItemID.ShadowScale, itemCount);
            }
            if (arg == ItemID.BrainOfCthulhuBossBag)
            {
                int itemCount = Main.rand.Next(30, 60);
                player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.BrainOfCthulhuBossBag), ItemID.CrimtaneOre, itemCount);
                itemCount = Main.rand.Next(10, 20);
                player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.BrainOfCthulhuBossBag), ItemID.TissueSample, itemCount);
            }
        }
    }
}
