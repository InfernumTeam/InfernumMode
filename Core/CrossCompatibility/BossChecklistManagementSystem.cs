using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.Items.SummonItems;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class BossChecklistManagementSystem : ModSystem
    {
        internal static Mod BossChecklist;
        /// <summary>
        /// <b>Vanilla main bosses:</b><br />
        ///  1.0 = King Slime<br />
        ///  2.0 = Eye of Cthulhu<br />
        ///  3.0 = Eater of Worlds / Brain of Cthulhu<br />
        ///  4.0 = Queen Bee<br />
        ///  5.0 = Skeletron<br />
        ///  6.0 = Deerclops<br />
        ///  7.0 = Wall of Flesh<br />
        ///  8.0 = Queen Slime<br />
        ///  9.0 = The Twins<br />
        /// 10.0 = The Destroyer<br />
        /// 11.0 = Skeletron Prime<br />
        /// 12.0 = Plantera<br />
        /// 13.0 = Golem<br />
        /// 14.0 = Duke Fishron<br />
        /// 15.0 = Empress of Light<br />
        /// 16.0 = Betsy<br />
        /// 17.0 = Lunatic Cultist<br />
        /// 18.0 = Moon Lord
        /// </summary>
        public Dictionary<string, float> BossChecklistValues = new()
        {
            // Directly before Queen Slime
            {"Dreadnautilus", 7.9f },
            // A little bit after Astrum Deus.
            {"BereftVassal", 17.75f },
            // After Yharon
            {"PrimordialWyrm", 22.5f }
        };
        public override void PostSetupContent()
        {
            // Stop here if Boss Checklist is not enabled.
            if (!ModLoader.TryGetMod("BossChecklist", out BossChecklist))
                return;

            void Add(string type, string bossName, Func<bool> downed, List<int> npcIDs, Dictionary<string, object> Dict)
            {
                if ((string)BossChecklist.Call
                (
                    $"Log{type}",
                    Mod,
                    bossName,
                    BossChecklistValues[bossName],
                    downed,
                    npcIDs,
                    Dict // displayName, spawnInfo, collectibles (doesn't work?), limbs, availability, spawnItems, customPortrait, despawnMessage, overrideHeadTextures
                ) == "Success")
                {

                }
                else
                {
                    InfernumMode.Instance.Logger.Warn($"Failed to add {Mod.Name} Boss Checklist entry");
                }
            }
            Add(type: "Boss",
                bossName: "Dreadnautilus",
                npcIDs: [NPCID.BloodNautilus],
                downed: () => DownedBossSystem.downedDreadnautilus,
                Dict: new()
                {
                    { "displayName", Lang.GetNPCName(NPCID.BloodNautilus) },
                    { "spawnInfo", Language.GetText($"Mods.{Mod.Name}.NPCs.Dreadnautilus.BossChecklistIntegration.SpawnInfo") },
                    { "availability", () => InfernumMode.CanUseCustomAIs && Main.hardMode },
                    { "spawnItems", ModContent.ItemType<RedBait>() },
                    { "despawnMessage", Language.GetText($"Mods.{Mod.Name}.NPCs.Dreadnautilus.BossChecklistIntegration.DespawnMessage") },
                    { "overrideHeadTextures", $"{Mod.Name}/Content/BehaviorOverrides/BossAIs/Dreadnautilus/DreadnautilusMapIcon" }
                }
            );
            Add(type: "Boss",
                bossName: nameof(BereftVassal),
                npcIDs: [ModContent.NPCType<BereftVassal>(), ModContent.NPCType<GreatSandShark>()],
                downed: () => WorldSaveSystem.DownedBereftVassal,
                Dict: new()
                {
                    { "spawnInfo", Language.GetText($"Mods.{Mod.Name}.NPCs.BereftVassal.BossChecklistIntegration.SpawnInfo") },
                    { "availability", () => InfernumMode.CanUseCustomAIs && NPC.downedAncientCultist },
                    { "spawnItems", ModContent.ItemType<SandstormsCore>() },
                    { "despawnMessage", Language.GetText($"Mods.{Mod.Name}.NPCs.BereftVassal.BossChecklistIntegration.DespawnMessage") },
                }
            );
            Add(type: "Boss",
                bossName: nameof(CalamityMod.NPCs.PrimordialWyrm),
                npcIDs: [ModContent.NPCType<PrimordialWyrmHead>()],
                downed: () => DownedBossSystem.downedPrimordialWyrm,
                Dict: new()
                {
                    { "spawnInfo", Language.GetText($"Mods.{Mod.Name}.NPCs.PrimordialWyrm.BossChecklistIntegration.SpawnInfo") },
                    { "availability", () => InfernumMode.CanUseCustomAIs && Main.hardMode },
                    { "spawnItems", ModContent.ItemType<EvokingSearune>() },
                    { "despawnMessage", Language.GetText($"Mods.{Mod.Name}.NPCs.PrimordialWyrm.BossChecklistIntegration.DespawnMessage") },
                    {
                        "customPortrait", new Action<SpriteBatch, Rectangle, Color>((spriteBatch, rect, color) =>
                        {
                            Texture2D tex = Mod.Assets.Request<Texture2D>("Assets/BossTextures/PrimordialWyrm/PrimordialWyrm_BossChecklist", AssetRequestMode.ImmediateLoad).Value;
                            Rectangle sourceRect = tex.Bounds;
                            float scale = Math.Min(1f, (float)rect.Width / sourceRect.Width);
                            spriteBatch.Draw(tex, rect.Center.ToVector2(), sourceRect, color, 0f, sourceRect.Size() / 2, scale, SpriteEffects.None, 0);
                        })
                    },
                }
            );
        }

        public override void Unload()
        {
            BossChecklist = null;
        }
    }
}
