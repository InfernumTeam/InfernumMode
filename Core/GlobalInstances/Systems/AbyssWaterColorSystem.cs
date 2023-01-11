using CalamityMod.ILEditing;
using InfernumMode.Content.WorldGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class AbyssWaterColorSystem : ModSystem
    {
        public static int AbyssWaterID
        {
            get;
            internal set;
        }

        public static float WaterBlacknessInterpolant
        {
            get;
            set;
        } = 1f;

        public static float OrangeAbyssWaterInterpolant
        {
            get;
            set;
        }

        internal static VertexColors ChangeAbyssColors(VertexColors initialColor, int liquidType, Point p)
        {
            bool isAbyssWater = liquidType == AbyssWaterID;

            if (WorldSaveSystem.InPostAEWUpdateWorld && isAbyssWater)
            {
                Color acidWaterColor = new(62, 217, 145);
                Color abyssWaterColor = new(29, 15, 56);
                Color orangeWaterColor = new(204, 58, 9);

                float topLeftBrightness = initialColor.TopLeftColor.ToVector3().Length() * 0.5773f;
                float topRightBrightness = initialColor.TopRightColor.ToVector3().Length() * 0.5773f;
                float bottomLeftBrightness = initialColor.BottomLeftColor.ToVector3().Length() * 0.5773f;
                float bottomRightBrightness = initialColor.BottomRightColor.ToVector3().Length() * 0.5773f;

                float blacknessInterpolant = Utils.GetLerpValue(CustomAbyss.Layer2Top, CustomAbyss.Layer4Top, p.Y, true) * WaterBlacknessInterpolant * 0.44f;
                float sulphuricWaterInterpolant = Utils.GetLerpValue(CustomAbyss.Layer2Top - 24f, CustomAbyss.AbyssTop - 4f, p.Y, true);

                // Conditional exists for optimization purposes.
                if (sulphuricWaterInterpolant <= 0f)
                {
                    initialColor.TopLeftColor = abyssWaterColor;
                    initialColor.TopRightColor = abyssWaterColor;
                    initialColor.BottomLeftColor = abyssWaterColor;
                    initialColor.BottomRightColor = abyssWaterColor;
                }
                else
                {
                    initialColor.TopLeftColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant);
                    initialColor.TopRightColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant);
                    initialColor.BottomLeftColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant);
                    initialColor.BottomRightColor = Color.Lerp(abyssWaterColor, acidWaterColor, sulphuricWaterInterpolant);
                }

                if (blacknessInterpolant > 0f)
                {
                    initialColor.TopLeftColor = Color.Lerp(initialColor.TopLeftColor, Color.Black, blacknessInterpolant);
                    initialColor.TopRightColor = Color.Lerp(initialColor.TopRightColor, Color.Black, blacknessInterpolant);
                    initialColor.BottomLeftColor = Color.Lerp(initialColor.BottomLeftColor, Color.Black, blacknessInterpolant);
                    initialColor.BottomRightColor = Color.Lerp(initialColor.BottomRightColor, Color.Black, blacknessInterpolant);
                }
                
                float orangeWaterInterpolant = OrangeAbyssWaterInterpolant;
                if (orangeWaterInterpolant > 0f)
                {
                    initialColor.TopLeftColor = Color.Lerp(abyssWaterColor, orangeWaterColor, orangeWaterInterpolant);
                    initialColor.TopRightColor = Color.Lerp(abyssWaterColor, orangeWaterColor, orangeWaterInterpolant);
                    initialColor.BottomLeftColor = Color.Lerp(abyssWaterColor, orangeWaterColor, orangeWaterInterpolant);
                    initialColor.BottomRightColor = Color.Lerp(abyssWaterColor, orangeWaterColor, orangeWaterInterpolant);
                }
                initialColor.TopLeftColor *= topLeftBrightness;
                initialColor.TopRightColor *= topRightBrightness;
                initialColor.BottomLeftColor *= bottomLeftBrightness;
                initialColor.BottomRightColor *= bottomRightBrightness;
            }

            return initialColor;
        }

        private static void ChangeWaterQuadColors(ILContext il)
        {
            ILCursor cursor = new(il);
            MethodInfo textureGetValueMethod = typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < 2; i++)
            {
                if (!cursor.TryGotoNext(MoveType.After, c => c.MatchCallOrCallvirt("MonoMod.Cil.RuntimeILReferenceBag/FastDelegateInvokers", "Invoke")))
                    return;
            }

            // Pass the texture in so that the method can ensure it is not messing around with non-lava textures.
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(LiquidRenderer).GetField("_liquidTextures"));
            cursor.Emit(OpCodes.Ldloc, 8);
            cursor.Emit(OpCodes.Ldelem_Ref);
            cursor.Emit(OpCodes.Ldloc, 8);
            cursor.Emit(OpCodes.Ldloc, 3);
            cursor.Emit(OpCodes.Ldloc, 4);
            cursor.EmitDelegate<Func<VertexColors, Texture2D, int, int, int, VertexColors>>((initialColor, initialTexture, liquidType, x, y) =>
            {
                return ChangeAbyssColors(initialColor, liquidType, new(x, y));
            });
        }

        public override void OnModLoad()
        {
            if (Main.netMode != NetmodeID.Server)
                AbyssWaterID = ModContent.Find<ModWaterStyle>("InfernumMode/AbyssWater").Slot;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += ChangeWaterQuadColors;
        }

        public override void Unload()
        {
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw -= ChangeWaterQuadColors;
        }

        public override void PreUpdateEntities()
        {
            WaterBlacknessInterpolant = MathHelper.Clamp(WaterBlacknessInterpolant + 0.02f, 0f, 1f);
        }
    }
}