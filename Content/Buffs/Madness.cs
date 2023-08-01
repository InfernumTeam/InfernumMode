using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class Madness : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Madness");
            // Description.SetDefault("Going insane...");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) => player.Infernum_Debuff().Madness = true;
    }
}
