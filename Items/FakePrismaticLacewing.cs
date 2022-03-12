using InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class FakePrismaticLacewing : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fake Prismatic Lacewing");
            Tooltip.SetDefault("Summons the Empress of Light\n" +
                "lol\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 34;
            item.rare = ItemRarityID.Green;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.consumable = false;
            item.maxStack = 999;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<EmpressOfLightNPC>()) && !Main.dayTime;

        public override bool UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 400f;
                NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<EmpressOfLightNPC>());
            }
            return true;
        }
    }
}
