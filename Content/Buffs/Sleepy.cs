using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class Sleepy : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sleepy");
            Description.SetDefault("You are fast asleep!");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.cursed = true;
            ScreenObstruction.screenObstruction = 1f;
            player.blackout = true;
            player.webbed = true;
        }
    }
}
