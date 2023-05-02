using CalamityMod;
using CalamityMod.Items.Dyes;
using CalamityMod.Items.Materials;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Dyes
{
    public class ProfanedCrystalDye : BaseDye
    {
        public override ArmorShaderData ShaderDataToBind
        {
            get
            {
                var shader = new ArmorShaderData(new Ref<Effect>(Mod.Assets.Request<Effect>("Assets/Effects/Dyes/ProfanedCrystalDyeShader", AssetRequestMode.ImmediateLoad).Value), "DyePass").UseColor(new Color(223, 155, 233));
                shader.SetShaderTextureArmor(InfernumTextureRegistry.CrystalNoise);
                return shader;
            }
        }

        public override void Load()
        {
            On.Terraria.Graphics.Shaders.ArmorShaderData.Apply += UseNormalMap;
        }

        private void UseNormalMap(On.Terraria.Graphics.Shaders.ArmorShaderData.orig_Apply orig, ArmorShaderData self, Entity entity, Terraria.DataStructures.DrawData? drawData)
        {
            if (self == GameShaders.Armor.GetShaderFromItemId(Type))
            {
                Main.graphics.GraphicsDevice.Textures[2] = InfernumTextureRegistry.CrystalNoiseNormal.Value;
                self.Shader.Parameters["uImageSize2"].SetValue(InfernumTextureRegistry.CrystalNoiseNormal.Value.Size());
            }

            orig(self, entity, drawData);
        }

        public override void SafeSetStaticDefaults()
        {
            SacrificeTotal = 3;
            DisplayName.SetDefault("Profaned Crystal Dye");
        }

        public override void SafeSetDefaults()
        {
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(0, 2, 50, 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe(5).
                AddIngredient(ItemID.BottledWater, 5).
                AddIngredient<DivineGeode>().
                AddTile(TileID.DyeVat).
                Register();
        }
    }
}
