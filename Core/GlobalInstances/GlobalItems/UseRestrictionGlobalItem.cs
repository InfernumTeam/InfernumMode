using CalamityMod.CalPlayer;
using CalamityMod.Events;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.GlobalItems
{
    public class UseRestrictionGlobalItem : GlobalItem
    {
        public static void DisplayDoGTeleportDenialText(Player player, Item item)
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
                DisplayDoGTeleportDenialText(player, item);
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
    }
}
