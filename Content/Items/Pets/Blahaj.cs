using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles.Pets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    public class Blahaj : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Blahaj");
            Tooltip.SetDefault("Summons a pet ikea plushie");
        }
        public override void SetDefaults()
        {
            Item.damage = 0;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noMelee = true;
            Item.width = 30;
            Item.height = 30;

            Item.value = Item.sellPrice(copper: 69);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<BlahajProj>();
            Item.buffType = ModContent.BuffType<BlahajBuff>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 15, true);
        }
    }
}
