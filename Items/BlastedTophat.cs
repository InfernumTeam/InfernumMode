using CalamityMod.Projectiles.Pets;
using InfernumMode.Buffs;
using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class BlastedTophat : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Blasted Tophat");
            Tooltip.SetDefault("Summons a small hat girl\n" +
                "Upon respawning after a boss battle, she sometimes gives you advice");
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

            Item.value = Item.sellPrice(platinum: 1);
            Item.rare = ItemRarityID.Pink;

            Item.shoot = ModContent.ProjectileType<HatGirl>();
            Item.buffType = ModContent.BuffType<HatGirlBuff>();
            Item.UseSound = SoundID.Meowmere;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 15, true);
        }
    }
}
