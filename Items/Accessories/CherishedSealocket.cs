using CalamityMod;
using CalamityMod.Items;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Cooldowns;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.Accessories
{
    public class CherishedSealocket : ModItem
    {

        public const int MaxHighDRHits = 2;

        public const int ForcefieldRechargeSeconds = 90;

        public const float ForcefieldDRMultiplier = 0.8f;

        public const float DamageBoostWhenForcefieldIsUp = 0.12f;

        public override void Load() => On.Terraria.Main.DrawInfernoRings += DrawForcefields;

        public override void Unload() => On.Terraria.Main.DrawInfernoRings += DrawForcefields;

        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Cherished Sealocket");
            Tooltip.SetDefault($"Grants a water forcefield that dissipates after {MaxHighDRHits} hits\n" +
                $"When the forcefield is up, hard-hitting hits do {ForcefieldDRMultiplier * 100f}% less damage overall, and you recieve a {(int)(DamageBoostWhenForcefieldIsUp * 100f)}% damage boost\n" +
                $"The forcefield reappears after {ForcefieldRechargeSeconds} seconds");
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ItemRarityID.Cyan;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            SealocketPlayer modPlayer = player.GetModPlayer<SealocketPlayer>();
            modPlayer.MechanicalEffectsApply = true;
            modPlayer.ForcefieldCanDraw = !hideVisual;
        }

        public override void UpdateVanity(Player player) =>
            player.GetModPlayer<SealocketPlayer>().ForcefieldCanDraw = true;

        private void DrawForcefields(On.Terraria.Main.orig_DrawInfernoRings orig, Main self)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active || Main.player[i].outOfRange || Main.player[i].dead)
                    continue;

                SealocketPlayer modPlayer = Main.player[i].GetModPlayer<SealocketPlayer>();
                if (modPlayer.ForcefieldOpacity <= 0.01f || modPlayer.ForcefieldDissipationInterpolant >= 0.99f)
                    continue;

                float forcefieldOpacity = (1f - modPlayer.ForcefieldDissipationInterpolant) * modPlayer.ForcefieldOpacity;
                Vector2 forcefieldDrawPosition = Main.player[i].Center + Vector2.UnitY * Main.player[i].gfxOffY - Main.screenPosition;
                BereftVassal.DrawElectricShield(forcefieldOpacity, forcefieldDrawPosition, forcefieldOpacity, modPlayer.ForcefieldDissipationInterpolant * 1.5f + 1.3f);
            }
            orig(self);
        }
    }
}
