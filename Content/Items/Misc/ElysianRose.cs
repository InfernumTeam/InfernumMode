using CalamityMod;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Content.Tiles.Wishes;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    // Dedicated to: LGL
    public class ElysianRose : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Elysian Rose");
            // Tooltip.SetDefault("It yearns for the deepest depths of the abyss...");
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<ElysianRoseTile>());

            Item.width = 18;
            Item.height = 18;
            Item.maxStack = 1;
            Item.value = 0;
            Item.rare = ModContent.RarityType<InfernumOceanFlowerRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            Player closestPlayer = Main.player[Player.FindClosest(Item.position, 1, 1)];
            bool inAbyss = closestPlayer.WithinRange(Item.position, 600f) && closestPlayer.Calamity().ZoneAbyssLayer4;
            if (inAbyss && Collision.WetCollision(Item.TopLeft, Item.width, Item.height))
            {
                // Create bubbles and magic.
                int numDust = 50;
                for (int i = 0; i < numDust; i++)
                {
                    Vector2 ringVelocity = (TwoPi * i / numDust).ToRotationVector2().RotatedBy(Item.velocity.ToRotation() + PiOver2) * 5f;
                    Dust ringDust = Dust.NewDustPerfect(Item.position, 211, ringVelocity, 100, default, 1.25f);
                    ringDust.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item165, Item.position);
                int oldStack = Item.stack;
                Item.SetDefaults(ModContent.ItemType<FlowerOfTheOcean>());
                Item.stack = oldStack;
            }
        }
    }
}
