using CalamityMod.NPCs.Polterghast;
using InfernumMode.Common.DataStructures;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class Madness : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) =>
            {
                player.SetValue<bool>("Madness", false);
            };

            InfernumPlayer.UpdateDeadEvent += (InfernumPlayer player) =>
            {
                player.SetValue<int>("MadnessTime", 0);
            };

            InfernumPlayer.PreKillEvent += (InfernumPlayer player, double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource) =>
            {
                if (player.GetValue<bool>("Madness"))
                    damageSource = damageSource = PlayerDeathReason.ByCustomReason(Utilities.GetLocalization("Status.Death.Madness").Format(player.Player.name));

                return true;
            };

            InfernumPlayer.UpdateLifeRegenEvent += (InfernumPlayer player) =>
            {
                Referenced<int> madnessTime = player.GetRefValue<int>("MadnessTime");

                if (player.GetValue<bool>("Madness"))
                {
                    int regenLoss = NPC.AnyNPCs(ModContent.NPCType<Polterghast>()) ? 800 : 50;
                    if (player.Player.lifeRegen > 0)
                        player.Player.lifeRegen = 0;
                    player.Player.lifeRegenTime = 0;
                    player.Player.lifeRegen -= regenLoss;
                }

                madnessTime.Value = Utils.Clamp(madnessTime.Value + (player.GetValue<bool>("Madness") ? 1 : -8), 0, 660);
            };
        }

        public override void Update(Player player, ref int buffIndex) => player.Infernum().SetValue<bool>("Madness", true);
    }
}
