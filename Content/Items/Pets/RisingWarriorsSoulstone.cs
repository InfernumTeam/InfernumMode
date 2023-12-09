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
    // Dedicated to: PurpleMattik
    public class RisingWarriorsSoulstone : ModItem
    {
        public static readonly Color TextColor = new(233, 124, 249);

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
