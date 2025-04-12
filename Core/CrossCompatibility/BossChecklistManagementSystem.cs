using System;
using System.Collections.Generic;
using CalamityMod.Items.Placeables.Furniture.DevPaintings;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs.GreatSandShark;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.Items.LoreItems;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Items.Placeables;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            // A little bit after Astrum Deus.
            {"BereftVassal", 17.75f}
        };
        public override void PostSetupContent()
        {
            ModLoader.TryGetMod("BossChecklist", out BossChecklist);

            // Stop here if Boss Checklist is not enabled.
            if (BossChecklist is null)
                return;

            void Add(string type, string bossName, List<int> npcIDs, Func<bool> downed, Func<bool> available, List<int> collectibles, List<int> spawnItems, string portrait = null)
            {
                BossChecklist.Call(
                    $"Log{type}",
                    Mod,
                    bossName,
                    BossChecklistValues[bossName],
                    downed,
                    npcIDs,
                    new Dictionary<string, object>()
                    {
                            { "spawnItems", spawnItems },
                            // { "collectibles", collectibles }, // it's fetched from npc loot? TODO: refactor method calls below
                            { "availability", available },
                            { "despawnMessage", Language.GetText($"Mods.{Name}.NPCs.{bossName}.BossChecklistIntegration.DespawnMessage") },
                            {
                                "customPortrait",
                                portrait == null ? null : new Action<SpriteBatch, Rectangle, Color>((spriteBatch, rect, color) =>
                                {
                                    Texture2D tex = Mod.Assets.Request<Texture2D>(portrait, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                                    Rectangle sourceRect = tex.Bounds;
                                    float scale = Math.Min(1f, (float)rect.Width / sourceRect.Width);
                                    spriteBatch.Draw(tex, rect.Center.ToVector2(), sourceRect, color, 0f, sourceRect.Size() / 2, scale, SpriteEffects.None, 0);
                                })
                            }
                    }
                );
            }
            Add("Boss",
                nameof(BereftVassal),
                [ModContent.NPCType<BereftVassal>(), ModContent.NPCType<GreatSandShark>()],
                () => WorldSaveSystem.DownedBereftVassal,
                () => NPC.downedAncientCultist,
                [
                    ModContent.ItemType<BereftVassalTrophy>(),
                    ModContent.ItemType<KnowledgeBereftVassal>(),
                    ModContent.ItemType<WaterglassToken>(),
                    ModContent.ItemType<ThankYouPainting>(),
                ],
                [ModContent.ItemType<SandstormsCore>()]
            );
        }

        public override void Unload()
        {
            BossChecklist = null;
        }
    }
}
