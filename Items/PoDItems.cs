using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.TreasureBags;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Items;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
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
                foreach (TooltipLine line2 in tooltips)
                {
                    if (line2.Mod == "Terraria" && line2.Name == "Tooltip0")
                    {
                        line2.Text = "Summons the Moon Lord immediately\n" +
                                     "Creates an arena at the player's position\n" +
                                     "Not consumable.";
                    }
                }
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedCore>())
            {
                foreach (TooltipLine line2 in tooltips)
                {
                    if (line2.Mod == "Terraria" && line2.Name == "Tooltip1")
                    {
                        line2.Text = "Summons Providence when used at the alter in the profaned temple at the far right of the underworld";
                    }
                }
            }

            if (item.type == ItemID.LihzahrdPowerCell)
            {
                foreach (TooltipLine line2 in tooltips)
                {
                    if (line2.Mod == "Terraria" && line2.Name == "Tooltip0")
                        line2.Text += "\nCreates a rectangular arena around the altar. If the altar is inside of the temple solid tiles within the arena are broken";
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
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.RodofDiscord && NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
            {
                DoGTeleportDenialText(player, item);
                return false;
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedCore>())
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

        public override void RightClick(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<StarterBag>())
            {
                DropHelper.DropItem(player.GetSource_OpenItem(item.type), player, ModContent.ItemType<BlastedTophat>());
                DropHelper.DropItemCondition(player.GetSource_OpenItem(item.type), player, ModContent.ItemType<DemonicChaliceOfInfernum>(), Main.expertMode);
            }
        }

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
