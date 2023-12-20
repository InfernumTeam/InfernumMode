using InfernumMode.Core.OverridingSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class TipsManager : ModSystem
    {
        internal static NPC BossBeingFought;

        internal static Dictionary<int, List<Func<NPC, string>>> TipsRegistry = new();

        internal static List<string> SaidText = new();

        public static string PotentialTipToUse
        {
            get;
            internal set;
        }

        public static bool ShouldUseJokeText => Main.rand.NextBool(300);

        public override void Unload()
        {
            TipsRegistry = null;
        }

        public override void PostUpdateEverything()
        {
            if (!Main.LocalPlayer.dead && !Main.LocalPlayer.Infernum().GetValue<bool>("ShouldDisplayTips"))
            {
                NPC foughtBoss = Utilities.CurrentlyFoughtBoss;
                BossBeingFought = foughtBoss is null ? null : foughtBoss.Clone() as NPC;
            }
        }

        public static string SelectTip()
        {
            if (BossBeingFought is null)
                return string.Empty;

            if (!NPCBehaviorOverride.BehaviorOverrides.TryGetValue(BossBeingFought.type, out var bossInfo))
                return string.Empty;

            bossInfo = NPCBehaviorOverride.BehaviorOverrides[bossInfo.NPCTypeToDeferToForTips ?? bossInfo.NPCOverrideType];

            // This func evaluates the state of the NPC in question, after it died.
            IEnumerable<Func<NPC, string>> potentialTips = bossInfo.GetTips();
            var possibleThingsToSay = potentialTips.Select(t => t(BossBeingFought)).Where(t => !string.IsNullOrEmpty(t) && !SaidText.Contains(t)).ToList();

            if (!possibleThingsToSay.Any())
                return string.Empty;

            string key = possibleThingsToSay.ElementAt(Main.rand.Next(possibleThingsToSay.Count));
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return Language.GetTextValue(key);
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["SaidText"] = SaidText;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            SaidText = (List<string>)tag.GetList<string>("SaidText");
        }
    }
}
