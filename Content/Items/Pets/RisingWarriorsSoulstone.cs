using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles;
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
        // LITERALLY inverse of hat girl...
        public static readonly string[] GenericThingsToSayOnDeath = new string[]
        {
            "Another day, another death, ay?",
            "You totally could have dodged, that, mate.",
            "Life is an endless cycle. Death after death, you return. And yet your fate is the same.",
            "Still keeping me around? Hey, no no, I'm not complaining. Carry on.",
            "Oh you died? Apologies, I was much too lost in my own thoughts to notice, kekeke",
            "You should die to explosions more. Always fun to see.",
            "It's quite easy to point out where you went wrong there, kekeke",
            "Just pick up your money and break your grave, and we can pretend that never happened. Agreed?",
            "I never thought I would have to travel with someone so prone to dying.",
            "Yeah, maybe uhhhhh don't do that again.",
            "If this was hardcore mode I would be on the floor laughing my guts out right now.",
            "Oh, you think THAT death was bad? Man, lemme tell you about this one time I...",
            "Hmm? No, no, I saw everything. I'm always watching.",
            "And up goes the death counter.",
            "For legal reasons, I have to tell you that I did NOT snap an awesome photo of your death just now.",
            "You got a bit too goofy there, bub.",
            "Ah, so silly!!!",
            "All good things must come to an end. Oh, and you too it seems.",
            "Oh damn, I thought you might have actually gone that time. But you are back. Again.",
            "I appreciate you for trying, at least.",
            "I don't think witnessing you perish time and time again will ever get old.",
            "You should hold onto Adrenaline more, it helps you tank a hit. If you can build it up, that is.",
            "Ah! Close... ish.",
            "You should give me your gear so I can show you how it's done.",
            "Awesome. Now try again. I wanna see the bloodshed.",
            "What, you want me to help you during the fight? Sorry, I'm just here for style points.",
            "Oh yeah, this boss was balanced to screw specifically you and JUST you over. Cruel world, I know.",
            "...eh? Oh sorry sorry, was just vibin' to the boss music! Uh, sorry for your death, try again, I guess.",
            "Wow, you... lasted longer than I thought you would!",
            "I'm wondering if... maybe you need a break.",
            "They'll tell your story for ages to come... as that guy who died. Or something.",
            "I feel like BIG SHOT would suit that boss, to be honest.",
            "Hey now, don't get salty... I'm sure that was only a BIT your fault."
        };

        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Rising Warrior's Soulstone");
            Tooltip.SetDefault("Summons a wolf starchild that... 'helps' you?\n" +
                "The tablet resonates with a strong legacy");
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
