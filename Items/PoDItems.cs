using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Ammo;
using CalamityMod.Items.Fishing.AstralCatches;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs.DevourerofGods;
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

            if (item.type == ModContent.ItemType<FlashBullet>())
                item.damage = 4;

            if (item.type == ModContent.ItemType<NapalmArrow>())
                item.damage = 9;

            if (item.type == ItemID.StarCannon)
                item.damage = 24;

            if (item.type == ModContent.ItemType<HivePod>())
                item.damage = 74;

            if (item.type == ModContent.ItemType<SkyfinBombers>())
                item.damage = 25;

            if (item.type == ModContent.ItemType<GacruxianMollusk>())
                item.damage = 9;

            if (item.type == ModContent.ItemType<SeasSearing>())
                item.damage = 39;

            if (item.type == ModContent.ItemType<HeavenfallenStardisk>())
                item.damage = 87;

            if (item.type == ModContent.ItemType<ResurrectionButterfly>())
                item.damage = 44;

            if (item.type == ModContent.ItemType<Skullmasher>())
                item.damage = 737;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.CelestialSigil)
            {
                foreach (TooltipLine line2 in tooltips)
                {
                    if (line2.mod == "Terraria" && line2.Name == "Tooltip0")
                    {
                        line2.text = "Summons the Moon Lord immediately\n" +
                                     "Creates an arena at the player's position\n" +
                                     "Not consumable.";
                    }
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
                Main.NewText(Main.rand.Next(possibleEdgyShitToSay), Color.Cyan);
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
        public override bool UseItem(Item item, Player player)
        {
            if (item.type == ItemID.CelestialSigil && !NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                NPC.NewNPC((int)player.Center.X, (int)player.Center.Y, NPCID.MoonLordCore);
            }
            return base.UseItem(item, player);
        }

        public override void RightClick(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<StarterBag>())
                DropHelper.DropItemCondition(player, ModContent.ItemType<Death2>(), Main.expertMode);
        }
    }
}
