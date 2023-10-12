using CalamityMod;
using CalamityMod.Items.SummonItems;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class TooltipChangeGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        public bool DeveloperItem;

        public static Dictionary<int, LocalizedText> EnrageTooltipReplacements => new()
        {
            [ModContent.ItemType<DecapoditaSprout>()] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.DecapoditaSprout"),
            [ItemID.WormFood] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.WormFood"),
            [ItemID.BloodySpine] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.BloodySpine"),
            [ModContent.ItemType<Teratoma>()] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.Teratoma"),
            [ModContent.ItemType<BloodyWormFood>()] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.BloodyWormFood"),
            [ModContent.ItemType<Seafood>()] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.Seafood"),
            [ItemID.MechanicalEye] = null,
            [ItemID.MechanicalSkull] = null,
            [ItemID.MechanicalWorm] = null,
            [ItemID.ClothierVoodooDoll] = null,
            [ModContent.ItemType<Abombination>()] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.Abombination"),
            [ModContent.ItemType<ExoticPheromones>()] = null,
            [ModContent.ItemType<NecroplasmicBeacon>()] = Utilities.GetLocalization("UI.EnrageTooltipReplacements.NecroplasmicBeacon")
        };

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            void replaceTooltipText(string tooltipIdentifier, string replacementText, Color? replacementColor = null)
            {
                var tooltip = tooltips.FirstOrDefault(x => x.Name == tooltipIdentifier && x.Mod == "Terraria");

                // Stop if the tooltip could not be identified.
                if (tooltip is null)
                    return;
                replacementColor ??= Color.White;
                tooltip.Text = replacementText;
                tooltip.OverrideColor = replacementColor;
            }

            void addTooltipLineAfterLine(string tooltipIdentifier, TooltipLine line)
            {
                int tooltipIndex = tooltips.FindIndex(x => x.Name == tooltipIdentifier && x.Mod == "Terraria");

                // Stop if the tooltip could not be identified.
                if (tooltipIndex == -1)
                    return;

                tooltips.Insert(tooltipIndex + 1, line);
            }

            if (item.type == ItemID.CelestialSigil)
            {
                LocalizedText summoningText = Utilities.GetLocalization("Items.CelestialSigil.SummoningText");
                replaceTooltipText("Tooltip0", summoningText.Value);
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedShard>())
            {
                bool inGarden = Main.LocalPlayer.Infernum_Biome().InProfanedArena;
                LocalizedText summoningText = Utilities.GetLocalization("Items.ProfanedShard.SummoningText");

                Color textColor = inGarden ? WayfinderSymbol.Colors[2] : Color.White;
                replaceTooltipText("Tooltip0", summoningText.Value, textColor); // Same here, old tooltip: "Tooltip1"

                // Remove the next line about an enrage condition.
                tooltips.RemoveAll(x => x.Name == "Tooltip2" && x.Mod == "Terraria");

                // Add a warning about the lack of a garden if it hasn't been generated yet.
                if (!WorldSaveSystem.HasGeneratedProfanedShrine)
                {
                    LocalizedText gardenWarning = Utilities.GetLocalization("Items.ProfanedShard.GardenWarning");
                    TooltipLine warningTooltip = new(Mod, "Warning", gardenWarning.Value)
                    {
                        OverrideColor = Color.Orange
                    };
                    addTooltipLineAfterLine("Tooltip1", warningTooltip);
                }
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<EyeofDesolation>())
            {
                string summoningText = Utilities.GetLocalization("Items.EyeofDesolation.SummoningText").Format(CalamitasShadowBehaviorOverride.CustomName);
                replaceTooltipText("Tooltip0", summoningText);
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedCore>())
            {
                LocalizedText summoningText = Utilities.GetLocalization("Items.ProfanedCore.SummoningText");
                replaceTooltipText("Tooltip0", summoningText.Value);
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<RuneofKos>() && WorldSaveSystem.ForbiddenArchiveCenter.X != 0)
            {
                LocalizedText summoningText = Utilities.GetLocalization("Items.RuneofKos.DeveloperText");
                TooltipLine warningLine = new(Mod, "CVWarning", CalamityUtils.ColorMessage(summoningText.Value, Color.Magenta));
                tooltips.Add(warningLine);
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.LihzahrdPowerCell)
            {
                LocalizedText summoningText = Utilities.GetLocalization("Items.LihzahrdPowerCell.SummoningText");
                replaceTooltipText("Tooltip0", summoningText.Value);
            }

            if (item.type == ModContent.ItemType<SandstormsCore>())
            {
                var tooltip0 = tooltips.FirstOrDefault(x => x.Name == "Tooltip0" && x.Mod == "Terraria");
                if (tooltip0 != null)
                {
                    if (WorldSaveSystem.HasGeneratedColosseumEntrance || SubworldSystem.IsActive<LostColosseum>())
                    {
                        tooltip0.Text = Utilities.GetLocalization("Items.SandstormsCore.PortalDetail").Value;
                        tooltip0.OverrideColor = Color.Lerp(Color.Orange, Color.Yellow, 0.55f);
                    }

                    // Warn the player about not having a colosseum entrance if it hasn't been generated yet.
                    else
                    {
                        tooltip0.Text = Utilities.GetLocalization("Items.SandstormsCore.GatewayWarning").Value;
                        tooltip0.OverrideColor = Color.Orange;
                    }
                }
                tooltips.RemoveAll(x => x.Name == "Tooltip1" && x.Mod == "Terraria");
            }

            // Remove a bunch of "Enrages in XYZ" tooltips from base Calamity because people keep getting confused by it.
            if (InfernumMode.CanUseCustomAIs)
                EditEnrageTooltips(item, tooltips);

            if (DeveloperItem)
            {
                Color devColor = CalamityUtils.ColorSwap(Color.OrangeRed, Color.DarkRed, 2f);
                TooltipLine developerLine = new(Mod, "Developer", Utilities.GetLocalization("Items.DeveloperItem").Format(devColor.Hex3()));
                tooltips.Add(developerLine);
            }
        }

        public static void EditEnrageTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Don't do anything if the item doesn't call for a tooltip replacement.
            if (!EnrageTooltipReplacements.TryGetValue(item.type, out var tooltipReplacement))
                return;

            // Don't do anything if the item has no enrage tooltip to reference.
            string localizedEnrageText = Utilities.GetLocalization("Items.EnrageTooltip").Value.ToLower();
            var enrageTooltip = tooltips.FirstOrDefault(x => x.Text.Contains(localizedEnrageText, StringComparison.OrdinalIgnoreCase));
            if (enrageTooltip is null)
                return;

            int enrageTextStart = enrageTooltip.Text.IndexOf("enrage", StringComparison.OrdinalIgnoreCase);
            int enrageTextEnd = enrageTextStart;

            // Find where the current line terminates following the instance of the word 'enrage'.
            while (enrageTextEnd < enrageTooltip.Text.Length && enrageTooltip.Text[enrageTextEnd] != '\n')
                enrageTextEnd++;

            enrageTooltip.Text = enrageTooltip.Text.Remove(enrageTextStart, Math.Min(enrageTextEnd - enrageTextStart, enrageTooltip.Text.Length));

            // If a replacement exists, insert it into the enrage text instead.
            if (tooltipReplacement is not null)
                enrageTooltip.Text = enrageTooltip.Text.Insert(enrageTextStart, tooltipReplacement.Value);
            else
                enrageTooltip.Text = enrageTooltip.Text.Replace("\n\n", "\n");
        }
    }
}
