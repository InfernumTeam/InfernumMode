using InfernumMode.Content.Dusts;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class DarkFlames : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) =>
            {
                player.SetValue<bool>("DarkFlames", false);
            };

            InfernumPlayer.UpdateLifeRegenEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>("DarkFlames"))
                {
                    if (player.Player.lifeRegen > 0)
                        player.Player.lifeRegen = 0;
                    player.Player.lifeRegenTime = 0;
                    player.Player.lifeRegen -= 30;

                    player.Player.statDefense -= 8;
                }
            };

            InfernumPlayer.PostUpdateEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>("DarkFlames"))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust shadowflame = Dust.NewDustDirect(player.Player.position, player.Player.width, player.Player.height, ModContent.DustType<RavagerMagicDust>());
                        shadowflame.velocity = player.Player.velocity.SafeNormalize(Vector2.UnitX * player.Player.direction);
                        shadowflame.velocity = shadowflame.velocity.RotatedByRandom(0.4f) * -Main.rand.NextFloat(2.5f, 5.4f);
                        shadowflame.velocity += Main.rand.NextVector2Circular(3f, 3f);
                        shadowflame.scale = Main.rand.NextFloat(0.95f, 1.25f);
                        shadowflame.noGravity = true;
                    }
                }
            };

            InfernumPlayer.PreKillEvent += (InfernumPlayer player, double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref Terraria.DataStructures.PlayerDeathReason damageSource) =>
            {
                if (damage == 10.0 && hitDirection == 0 && damageSource.SourceOtherIndex == 8)
                {
                    if (player.GetValue<bool>("DarkFlames"))
                        damageSource = PlayerDeathReason.ByCustomReason(Utilities.GetLocalization("Status.Death.DarkFlames").Format(player.Player.name));
                }

                return true;
            };
        }

        public override void Update(Player player, ref int buffIndex) => player.Infernum().SetValue<bool>("DarkFlames", true);
    }
}
