using InfernumMode.Content.Projectiles.Pets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class HatGirlBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hat Girl");
            // Description.SetDefault("Arson");
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            player.Infernum().SetValue<bool>("HatGirl", true);
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<HatGirl>()] <= 0;
            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, ModContent.ProjectileType<HatGirl>(), 0, 0f, player.whoAmI);
        }
    }
}
