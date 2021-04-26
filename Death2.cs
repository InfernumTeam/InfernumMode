using CalamityMod;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class Death2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hyperdeath");
            Tooltip.SetDefault("Makes bosses absurd unless Boss Rush is active\n" +
                               "Deathmode must be active to use this item");
        }

        public override void SetDefaults()
        {
            item.rare = ItemRarityID.Red;
            item.width = 28;
            item.height = 28;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.UseSound = SoundID.Item119;
            item.consumable = false;
        }

        public override bool CanUseItem(Player player)
        {
            if (!CalamityWorld.death || BossRushEvent.BossRushActive)
            {
                return false;
            }
            return true;
        }

        public override bool UseItem(Player player)
        {
            for (int doom = 0; doom < 200; doom++)
            {
                if (Main.npc[doom].active && (Main.npc[doom].boss || Main.npc[doom].type == NPCID.EaterofWorldsHead || Main.npc[doom].type == NPCID.EaterofWorldsTail || Main.npc[doom].type == mod.NPCType("SlimeGodRun") ||
                    Main.npc[doom].type == mod.NPCType("SlimeGodRunSplit") || Main.npc[doom].type == mod.NPCType("SlimeGod") || Main.npc[doom].type == mod.NPCType("SlimeGodSplit")))
                {
                    player.KillMe(PlayerDeathReason.ByOther(12), 1000.0, 0, false);
                    Main.npc[doom].active = false;
                    Main.npc[doom].netUpdate = true;
                }
            }
            if (!PoDWorld.InfernumMode)
            {
                PoDWorld.InfernumMode = true;
                Color messageColor = Color.Crimson;
                Main.NewText("Prepare for hell.", messageColor);
            }
            else
            {
                PoDWorld.InfernumMode = false;
                Color messageColor = Color.Crimson;
                Main.NewText("Very well then.", messageColor);
            }

            if (Main.netMode == NetmodeID.Server)
                CalamityNetcode.SyncWorld();
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
