using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class UseRestrictionGlobalItem : GlobalItem
    {
        public static bool DisplayTeleportDenialText(Player player, Vector2 teleportPosition, Item item, bool isDoG)
        {
            if (!player.chaosState)
            {
                player.AddBuff(BuffID.ChaosState, CalamityPlayer.chaosStateDuration, true);
                if (isDoG)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(item), teleportPosition, Vector2.Zero, ModContent.ProjectileType<RoDFailPulse>(), 0, 0f, player.whoAmI);

                    string[] possibleEdgyShitToSay = new string[]
                    {
                        "YOU CANNOT EVADE ME SO EASILY!",
                        "YOU CANNOT HOPE TO OUTSMART A MASTER OF DIMENSIONS!",
                        "NOT SO FAST!"
                    };
                    Utilities.DisplayText(Main.rand.Next(possibleEdgyShitToSay), Color.Cyan);
                    HatGirl.SayThingWhileOwnerIsAlive(player, "It seems as if it is manipulating telelocational magic, your Rod of Discord is of no use here!");
                }
                else
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(item), teleportPosition, Vector2.Zero, ModContent.ProjectileType<GuardiansRodFailPulse>(), 0, 0f, player.whoAmI);
                    HatGirl.SayThingWhileOwnerIsAlive(player, "The profaned magic seems to be blocking your Rod of Discord!");
                }
            }
            return false;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.RodofDiscord)
            {
                if (NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()) || Main.projectile.Any(p => p.active && p.type == ModContent.ProjectileType<GuardiansSummonerProjectile>()))
                    return DisplayTeleportDenialText(player, Main.MouseWorld, item, false);
                if (NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
                    return DisplayTeleportDenialText(player, Main.MouseWorld, item, true);
            }

            if (InfernumMode.CanUseCustomAIs && (item.type == ModContent.ItemType<ProfanedCore>() || item.type == ModContent.ItemType<SandstormsCore>()))
                return false;

            // Only allow the profaned shard to be used in the correct area.
            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedShard>())
                return player.Hitbox.Intersects(GuardianComboAttackManager.ShardUseisAllowedArea) && !WeakReferenceSupport.InAnySubworld();

            bool inArena = player.Infernum_Biome().InProfanedArenaAntiCheeseZone || SubworldSystem.IsActive<LostColosseum>();
            bool illegalItemForArena = item.type is ItemID.Sandgun or ItemID.DirtBomb or ItemID.DirtStickyBomb or ItemID.DryBomb;
            if (illegalItemForArena && inArena)
                return false;

            bool inAbyss = InfernumMode.CanUseCustomAIs && (player.Calamity().ZoneAbyssLayer3 || player.Calamity().ZoneAbyssLayer4);
            if (inAbyss && (item.type is ItemID.RecallPotion or ItemID.IceMirror or ItemID.MagicConch or ItemID.DemonConch))
                return false;

            return base.CanUseItem(item, player);
        }

        public override bool? UseItem(Item item, Player player)
        {
            // Disable magic mirror teleportation effects in the lower layers of the abyss.
            bool inAbyss = InfernumMode.CanUseCustomAIs && (player.Calamity().ZoneAbyssLayer3 || player.Calamity().ZoneAbyssLayer4);
            bool spawnTeleportingItem = item.type is ItemID.MagicMirror or ItemID.CellPhone;
            if (spawnTeleportingItem && inAbyss)
            {
                if (player.itemAnimation >= item.useAnimation - 2)
                    CombatText.NewText(player.Hitbox, Color.Navy, "The pressure is too strong to escape!", true);
                player.itemTime = item.useTime / 2 - 1;
            }

            return base.UseItem(item, player);
        }

        public override void UpdateInventory(Item item, Player player)
        {
            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<NormalityRelocator>())
            {
                if (NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()))
                    player.Calamity().normalityRelocator = false;
            }
        }
    }
}
