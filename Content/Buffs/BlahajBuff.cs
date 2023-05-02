using InfernumMode.Content.Projectiles.Pets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class BlahajBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blahaj");
            Description.SetDefault("Comforting friend");
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            player.Infernum_Pet().BlahajPet = true;
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<BlahajProj>()] <= 0;
            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Top - Vector2.UnitY * 50f, Vector2.Zero, ModContent.ProjectileType<BlahajProj>(), 0, 0f, player.whoAmI);
        }
    }
}
