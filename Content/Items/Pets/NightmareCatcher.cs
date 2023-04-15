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
    public class NightmareCatcher : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Nightmare Catcher");
            Tooltip.SetDefault("Summons the Sheep Lord to follow you around\n" +
                "It Appears From the Darkness, As I Softly Slink Into The Dream");
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
