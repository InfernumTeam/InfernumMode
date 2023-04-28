using CalamityMod;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class CherryBlossomDrawer : ModSystem
    {
        public static ManagedRenderTarget CherryBlossomTarget
        {
            get;
            internal set;
        }

        public static ArmorShaderData CherryBlossomShader
        {
            get;
            internal set;
        }

        public override void OnModLoad()
        {
            CherryBlossomTarget ??= new(true, RenderTargetManager.CreateScreenSizedTarget);
            DyeFindingSystem.FindDyeEvent += FindCherryBlossomDye;
            Main.OnPreDraw += PrepareCherryBlossomTarget;
            On.Terraria.Main.DrawProjectiles += DrawCherryBlossoms;
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareCherryBlossomTarget;
            On.Terraria.Main.DrawProjectiles -= DrawCherryBlossoms;
        }

        private void FindCherryBlossomDye(Item armorItem, Item dyeItem)
        {
            if (armorItem.type == ModContent.ItemType<SakuraBloom>())
                CherryBlossomShader = GameShaders.Armor.GetShaderFromItemId(dyeItem.type);
        }

        private void PrepareCherryBlossomTarget(GameTime obj)
        {
            // Don't waste resources if the player has no cherry blossoms to draw.
            if (Main.gameMenu || Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<CherryBlossomPetal>()] <= 0)
                return;

            Main.instance.GraphicsDevice.SetRenderTarget(CherryBlossomTarget.Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            // Find all petals.
            int petalID = ModContent.ProjectileType<CherryBlossomPetal>();
            List<Projectile> petals = new();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                // Don't draw projectiles that aren't petals, are dead, or don't belong to the current client (aka don't draw someone else's petals and have them clutter the screen).
                if (p.type != petalID || !p.active || p.owner != Main.myPlayer)
                    continue;

                petals.Add(p);
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            foreach (Projectile p in petals)
            {
                p.hide = false;
                Color color = Color.White;
                p.ModProjectile<CherryBlossomPetal>().PreDraw(ref color);
                p.hide = true;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            foreach (Projectile p in petals)
            {
                p.hide = false;
                p.ModProjectile<CherryBlossomPetal>().AdditiveDraw(Main.spriteBatch);
                p.hide = true;
            }
            Main.spriteBatch.End();

            Main.instance.GraphicsDevice.SetRenderTarget(null);
        }

        private void DrawCherryBlossoms(On.Terraria.Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            // Don't waste resources if the player has no cherry blossoms to draw.
            if (Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<CherryBlossomPetal>()] <= 0)
                return;

            // Draw the render target, optionally with a dye shader.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            CherryBlossomShader?.Apply(null, new(CherryBlossomTarget.Target, Vector2.Zero, null, Color.White));
            Main.spriteBatch.Draw(CherryBlossomTarget.Target, Main.screenLastPosition - Main.screenPosition + CherryBlossomTarget.Target.Size() * 0.5f, null, Color.White, 0f, CherryBlossomTarget.Target.Size() * 0.5f, 1f, 0, 0f);
            Main.spriteBatch.End();
        }
    }
}