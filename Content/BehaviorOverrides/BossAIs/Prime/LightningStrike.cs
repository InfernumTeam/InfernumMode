using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class LightningStrike : BasePrimitiveLightningProjectile
    {
        public override int Lifetime => 90;
        public override int TrailPointCount => 150;

        public override void PostAI()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == NPCID.SkeletronPrime && Main.npc[i].active && Projectile.Hitbox.Intersects(Main.npc[i].Hitbox))
                {
                    Main.npc[i].Infernum().ExtraAI[0] = 1f;
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public override float PrimitiveWidthFunction(float completionRatio)
        {
            float baseWidth = Lerp(4f, 10f, Sin(Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return baseWidth * Sin(Pi * completionRatio);
        }

        public override Color PrimitiveColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.Crimson, Color.DarkRed, Sin(TwoPi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(baseColor, Color.Red, (Sin(Pi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.8f);
        }
    }
}
