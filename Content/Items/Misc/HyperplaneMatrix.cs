using CalamityMod;
using CalamityMod.Items;
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.DataStructures;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Players;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    public class HyperplaneMatrix : ModItem
    {
        public static bool CanBeUsed => (DownedBossSystem.downedCalamitas && DownedBossSystem.downedExoMechs) || Main.LocalPlayer.name == "Dominic";

        // How much the player gets hurt for if the matrix explodes due to being able to be used.
        public const int UnableToBeUsedHurtDamage = 500;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            InfernumPlayer.FreeDodgeEvent += (InfernumPlayer player, Player.HurtInfo info) =>
            {
                Referenced<int> hurtSoundCooldown = player.GetRefValue<int>("HurtSoundCooldown");
                if (player.GetValue<bool>("CyberneticImmortalityIsActive") && hurtSoundCooldown.Value <= 0)
                {
                    hurtSoundCooldown.Value = 60;
                    SoundEngine.PlaySound(InfernumSoundRegistry.AresTeslaShotSound, player.Player.Center);
                    info.SoundDisabled = true;
                }

                return player.GetValue<bool>("CyberneticImmortalityIsActive");
            };
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<InfernumHyperplaneMatrixRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<HyperplaneMatrixProjectile>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var poemTooltips = tooltips.Where(x => x.Name.Contains("Tooltip") && x.Mod == "Terraria");
            foreach (var tooltip in poemTooltips)
            {
                int tooltipLineIndex = (int)char.GetNumericValue(tooltip.Name.Last());
                if (tooltipLineIndex >= 2)
                    tooltip.OverrideColor = Draedon.TextColor;
            }
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
