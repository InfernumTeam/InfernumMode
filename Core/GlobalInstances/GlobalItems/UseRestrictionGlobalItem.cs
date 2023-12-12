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
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
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

                    CalamityUtils.DisplayLocalizedText($"Mods.InfernumMode.Status.DoGTeleportDenial{Main.rand.Next(1, 4)}", Color.Cyan);
                    HatGirl.SayThingWhileOwnerIsAlive(player, "Mods.InfernumMode.PetDialog.DoGRoDTip");
                }
                else
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(item), teleportPosition, Vector2.Zero, ModContent.ProjectileType<GuardiansRodFailPulse>(), 0, 0f, player.whoAmI);
                    HatGirl.SayThingWhileOwnerIsAlive(player, "Mods.InfernumMode.PetDialog.ProfanedRoDTip");
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

            bool inArena = player.Infernum_Biome().InProfanedArenaAntiCheeseZone || SubworldSystem.IsActive<LostColosseum>();
            bool illegalItemForArena = item.type is ItemID.Sandgun or ItemID.DirtBomb or ItemID.DirtStickyBomb or ItemID.DryBomb;
            if (illegalItemForArena && inArena)
                return false;

            bool inAbyss = InfernumMode.CanUseCustomAIs && (player.Calamity().ZoneAbyssLayer3 || player.Calamity().ZoneAbyssLayer4);
            if (inAbyss && (item.type is ItemID.RecallPotion or ItemID.IceMirror or ItemID.MagicConch or ItemID.DemonConch))
                return false;

            // Don't let tiles be placed in the profaned garden.
            var noPlaceRect = WorldSaveSystem.ProvidenceArena.ToWorldCoords();
            noPlaceRect.Inflate(2, 2);
            if ((item.createTile != -1 || item.createWall != -1) && noPlaceRect.Contains(player.Calamity().mouseWorld.ToPoint()))
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
                    CombatText.NewText(player.Hitbox, Color.Navy, Language.GetTextValue("Mods.InfernumMode.Status.AbyssTeleportAwayDenial"), true);
                player.itemTime = item.useTime / 2 - 1;
            }

            return base.UseItem(item, player);
        }

        public override void UpdateInventory(Item item, Player player)
        {
            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<NormalityRelocator>())
            {
                if (NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()) || NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
                    player.Calamity().normalityRelocator = false;
            }
        }
    }
}
