using CalamityMod;
using CalamityMod.CalPlayer;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    public class Wayfinder : ModItem
    {
        public int FrameCounter;

        public int Frame;

        public override string Texture => GetTexture();

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("The Wayfinder");
            // Tooltip.SetDefault("Creates a magical gate that allows you to fast travel to it\nDoes not work when a boss is alive\nModifedInModifyTooltips");
            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 8));
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 60;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ModContent.RarityType<InfernumProfanedRarity>();
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ProjectileID.ConfettiGun;
        }
        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) => !CalamityPlayer.areThereAnyDamnBosses && !Main.projectile.Any((p) => p.active && p.owner == player.whoAmI && p.type == ModContent.ProjectileType<WayfinderHoldout>());

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            WayfinderHoldout.UseContext useContext;

            // If holding both up and down.
            if (KeybindSystem.WayfinderDestroyKey.Current)
                useContext = WayfinderHoldout.UseContext.Destroy;
            // If holding up.
            else if (KeybindSystem.WayfinderCreateKey.Current)
                useContext = WayfinderHoldout.UseContext.Create;
            // If just normally using.
            else
                useContext = WayfinderHoldout.UseContext.Teleport;

            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<WayfinderHoldout>(), 0, 0, player.whoAmI, 0, (float)useContext);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine l in tooltips)
            {
                if (l.Text == null)
                    continue;

                Color mainColor = CalamityUtils.ColorSwap(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 4);

                if (l.Text.StartsWith("ModifedInModifyTooltips"))
                {
                    l.Text = $"Hold LMB to teleport to the gate" +
                    $"\nHold LMB and {KeybindSystem.WayfinderCreateKey.GetAssignedKeys().FirstOrDefault() ?? "[NONE]"} to set the gate to your position" +
                    $"\nHold LMB and {KeybindSystem.WayfinderDestroyKey.GetAssignedKeys().FirstOrDefault() ?? "[NONE]"} to remove the gate";
                    l.OverrideColor = mainColor;
                }
            }

        }

        public static string GetTexture()
        {
            if (WorldSaveSystem.WayfinderGateLocation != Vector2.Zero)
                return "InfernumMode/Content/Items/Misc/WayfinderAlt";
            return "InfernumMode/Content/Items/Misc/Wayfinder";
        }

        #region Drawing
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.AsyncLoad).Value;
            if (WorldSaveSystem.WayfinderGateLocation != Vector2.Zero)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * 2;
                    Color afterimageColor = new Color(1f, 0.6f, 0.4f, 0f) * 0.7f;
                    Main.spriteBatch.Draw(texture, position + afterimageOffset, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, false), afterimageColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
                spriteBatch.Draw(texture, position, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, false), Color.White, 0f, origin, scale, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

                DrawData drawData = new(texture, position, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, false), drawColor * 0.1f, 0f, origin, scale, SpriteEffects.None, 0);
                InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/WayfinderLayer"));
                InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);

                drawData.Draw(spriteBatch);

                spriteBatch.End();
                PlayerInput.SetZoom_UI();
                Matrix transformMatrix = Main.UIScaleMatrix;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, transformMatrix);
            }

            spriteBatch.Draw(texture, position, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8), Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            if (WorldSaveSystem.WayfinderGateLocation != Vector2.Zero)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * 4;
                    Color afterimageColor = new Color(1f, 0.6f, 0.4f, 0f) * 0.7f;
                    Main.spriteBatch.Draw(texture, Item.position - Main.screenPosition + afterimageOffset, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, false), afterimageColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                spriteBatch.Draw(texture, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, false), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                spriteBatch.EnterShaderRegion();

                DrawData drawData = new(texture, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, true), lightColor * 0.1f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
                InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/ScrollingLayers/WayfinderLayer"));
                InfernumEffectsRegistry.RealityTear2Shader.Apply(drawData);

                drawData.Draw(spriteBatch);
                spriteBatch.ExitShaderRegion();
                return false;
            }

            spriteBatch.Draw(texture, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8), lightColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            spriteBatch.Draw(texture, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref Frame, ref FrameCounter, 6, 8, frameCounterUp: false), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        #endregion
    }
}
