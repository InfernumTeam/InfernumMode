using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items.TreasureBags;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.DoG;
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

            if (item.type == ItemID.LihzahrdPowerCell)
            {
                foreach (TooltipLine line2 in tooltips)
                {
                    if (line2.Mod == "Terraria" && line2.Name == "Tooltip0")
                        line2.Text += "\nCreates a rectangular arena around the altar. If the altar is inside of the temple solid tiles within the arena are broken";
                }
            }
        }

        internal static void DoGTeleportDenialText(Player player)
        {
            if (!player.chaosState)
            {
                player.AddBuff(BuffID.ChaosState, CalamityPlayer.chaosStateDurationBoss, true);
                Projectile.NewProjectile(Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<RoDFailPulse>(), 0, 0f, player.whoAmI);

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
            if (item.type == ItemID.RodofDiscord && (NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>())))
            {
                if (PoDWorld.InfernumMode)
                {
                    DoGTeleportDenialText(player);
                    return false;
                }
            }
            return base.CanUseItem(item, player);
        }
        public override bool? UseItem(Item item, Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (item.type == ItemID.CelestialSigil && !NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)player.Center.X, (int)player.Center.Y, NPCID.MoonLordCore);
            }
            return base.UseItem(item, player);
        }

        public override void RightClick(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<StarterBag>())
                DropHelper.DropItemCondition(player, ModContent.ItemType<Death2>(), Main.expertMode);
        }

        public override void OpenVanillaBag(string context, Player player, int arg)
        {
            // Only apply bag drop contents in Infernum Mode and on boss bags.
            if (context != "bossBag" || !InfernumMode.CanUseCustomAIs)
                return;

            if (arg == ItemID.EaterOfWorldsBossBag)
            {
                int itemCount = Main.rand.Next(30, 60);
                player.QuickSpawnItem(ItemID.DemoniteOre, itemCount);
                itemCount = Main.rand.Next(10, 20);
                player.QuickSpawnItem(ItemID.ShadowScale, itemCount);
            }
            if (arg == ItemID.BrainOfCthulhuBossBag)
            {
                int itemCount = Main.rand.Next(30, 60);
                player.QuickSpawnItem(ItemID.CrimtaneOre, itemCount);
                itemCount = Main.rand.Next(10, 20);
                player.QuickSpawnItem(ItemID.TissueSample, itemCount);
            }
        }

        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (!PoDPlayer.ApplyEarlySpeedNerfs)
                return;

            if (item.prefix == PrefixID.Quick2)
                player.moveSpeed -= 0.02f;
        }
    }
}
