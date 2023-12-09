using CalamityMod;
using InfernumMode.Common.DataStructures;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    // Dedicated to: Shade__Storm
    public class NightmareCatcher : ModItem
    {
        public const int AchievementSleepTime = 15;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.PostUpdateEvent += (InfernumPlayer player) =>
            {
                Referenced<int> brimstoneCragsSleepTimer = player.GetRefValue<int>("BrimstoneCragsSleepTimer");

                if (player.Player.sleeping.FullyFallenAsleep && player.Player.Calamity().ZoneCalamity)
                    brimstoneCragsSleepTimer.Value++;
                else
                    brimstoneCragsSleepTimer.Value = 0;

                // Apply the achievement if sleeping for long enough.
                if (brimstoneCragsSleepTimer.Value >= CalamityUtils.SecondsToFrames(AchievementSleepTime))
                {
                    AchievementPlayer.ExtraUpdateHandler(player.Player, AchievementUpdateCheck.NightmareCatcher);
                    brimstoneCragsSleepTimer.Value = 0;
                }
            };
        }
        public override void SetDefaults()
        {
            Item.damage = 0;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noMelee = true;
            Item.width = 50;
            Item.height = 58;

            Item.value = Item.sellPrice(gold: 10);

            Item.rare = ModContent.RarityType<InfernumRedSparkRarity>();
            Item.shoot = ModContent.ProjectileType<SheepGod>();
            Item.buffType = ModContent.BuffType<SheepGodBuff>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine obj = tooltips.FirstOrDefault((x) => x.Name == "Tooltip1" && x.Mod == "Terraria");
            obj.OverrideColor = new(102, 0, 4);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 15, true);
        }
    }
}
