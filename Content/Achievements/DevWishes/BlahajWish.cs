using InfernumMode.Content.Items.Pets;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class BlahajWish : Achievement
    {
        public int FishronFightTimer;

        public const int MaxFightTimerLength = 3600;

        public override string LocalizationCategory => "Achievements.Wishes";

        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 15;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            IsDevWish = true;
        }

        public override void Update()
        {
            if (AchievementPlayer.DukeFishronDefeated)
            {
                AchievementPlayer.DukeFishronDefeated = false;
                if (FishronFightTimer < MaxFightTimerLength)
                {
                    CurrentCompletion++;
                    return;
                }

                FishronFightTimer = 0;
            }
            else
            {
                if (NPC.AnyNPCs(NPCID.DukeFishron))
                    FishronFightTimer++;
                else
                    FishronFightTimer = 0;
            }
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<Blahaj>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["BlahajCurrentCompletion"] = CurrentCompletion;
            tag["BlahajDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("BlahajCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("BlahajDoneCompletionEffects");
        }
    }
}
