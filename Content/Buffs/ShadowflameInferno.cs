using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Buffs
{
    public class ShadowflameInferno : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;

            InfernumPlayer.ResetEffectsEvent += (InfernumPlayer player) =>
            {
                player.SetValue<bool>("ShadowflameInferno", false);
            };

            InfernumPlayer.UpdateLifeRegenEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>("ShadowflameInferno"))
                {
                    if (player.Player.lifeRegen > 0)
                        player.Player.lifeRegen = 0;
                    player.Player.lifeRegenTime = 0;
                    player.Player.lifeRegen -= 23;
                }
            };

            InfernumPlayer.PostUpdateEvent += (InfernumPlayer player) =>
            {
                if (player.GetValue<bool>("ShadowflameInferno"))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Dust shadowflame = Dust.NewDustDirect(player.Player.position, player.Player.width, player.Player.height, DustID.Clay);
                        shadowflame.velocity = player.Player.velocity.SafeNormalize(Vector2.UnitX * player.Player.direction);
                        shadowflame.velocity = shadowflame.velocity.RotatedByRandom(0.4f) * -Main.rand.NextFloat(2.5f, 5.4f);
                        shadowflame.scale = Main.rand.NextFloat(0.95f, 1.3f);
                        shadowflame.noGravity = true;
                    }
                }
            };
        }

        public override void Update(Player player, ref int buffIndex) => player.Infernum().SetValue<bool>("ShadowflameInferno", true);
    }
}
