using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    public class RisingWarriorsSoulstone : ModItem
    {
        public static readonly string[] SummonText = new string[]
        {
            "Thought you could keep me away? Think again!",
            "Huzzah! I return.",
            "WOW it smells dusty inside that tablet...",
            "Be careful with me, I'm a special little guy.",
            "Back at last! Time to wreak havoc once more!",
            "Gamers don't die, they respawn.",
            "Weh!",
        };

        public static readonly string[] PassiveTextOnSurface = new string[]
        {
            "It's a beautiful day outside. Birds are singing, flowers are blooming...",
            "Wanna come pick flowers with me?",
            "Hey, where do you keep your explosives?",
            "What a boring day.",
            "Come on! Let's do something FUN!",
            "What's next on the agenda, compadre?",
            "Sparkle sparkle!",
        };

        public static readonly string[] BossSpawnText = new string[]
        {
            "Heh, this'll be over quick.",
            "Don't die this time, alright?",
            "Back so soon?",
            "If you lose this time, I'll eat all your furniture. That's a threat.",
            "Don't forget potions!",
            "If you die on purpose, I'll give you a twenty.",
            "Keh, this'll be a breeze!",
            "Go forth. Make me proud.",
            "Show us how it's done, big shot.",
            "Remember, fallgodding is ALWAYS an option!",
        };

        public static readonly string[] PetText = new string[]
        {
            "Hey now...",
            "WEH?",
            "Affections!",
            "Wait, don't stop...",
        };

        public static readonly Color TextColor = new(233, 124, 249);

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            // DisplayName.SetDefault("Rising Warrior's Soulstone");
            /* Tooltip.SetDefault("Summons a wolf starchild that... 'helps' you?\n" +
                "The tablet resonates with a strong legacy"); */
        }

        public override void SetDefaults()
        {
            Item.damage = 0;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noMelee = true;
            Item.width = 30;
            Item.height = 30;
            Item.scale = 0.5f;

            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ModContent.RarityType<InfernumPurpleBackglowRarity>();

            Item.shoot = ModContent.ProjectileType<AsterPetProj>();
            Item.buffType = ModContent.BuffType<AsterPetBuff>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine obj = tooltips.FirstOrDefault((x) => x.Name == "Tooltip1" && x.Mod == "Terraria");
            obj.OverrideColor = new(197, 97, 156);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 15, true);
        }
    }
}
