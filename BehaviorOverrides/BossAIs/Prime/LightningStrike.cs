using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class LightningStrike : BasePrimitiveLightningProjectile
    {
        public override int Lifetime => 90;
        public override int TrailPointCount => 150;

        public override void PostAI()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == NPCID.SkeletronPrime && Main.npc[i].active && projectile.Hitbox.Intersects(Main.npc[i].Hitbox))
                {
                    Main.npc[i].Infernum().ExtraAI[0] = 1f;
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public override float PrimitiveWidthFunction(float completionRatio)
        {
            float baseWidth = MathHelper.Lerp(4f, 10f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * projectile.scale;
            return baseWidth * (float)Math.Sin(MathHelper.Pi * completionRatio);
        }

        public override Color PrimitiveColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.Crimson, Color.DarkRed, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f);
            return Color.Lerp(baseColor, Color.Red, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f) * 0.8f);
        }
    }
}
